using HomeChat.Backend.AIModels;
using HomeChat.Backend.Performances;
using LLama;
using LLama.Common;
using System.Data;

namespace HomeChat.Backend.Chats;

public interface IChat : IAsyncDisposable
{
    Task SelectModel(string modelShortName);
    Task LoadSelectedModel();
    Task<List<ModelDescription>> GetModels();
    Task Process(string prompt, int maxTokens, Func<string, Task> onNewText, CancellationToken cancellationToken);

    Task<(string modelShortName, string[] conversation)> GetConversation();
}

public class Chat(IModelCollection _modelCollection, ILogger<Chat> _logger, IPerformanceMonitor _performanceMonitor, ISessionCleanerService _sessionCleanerService) : IChat
{
    private LLamaWeights _model;
    private ChatSession _chatSession;
    private LLamaContext _context;
    private ModelDescription _currentModel;
    public bool IsFileReady(string filename)
    {
        // If the file can be opened for exclusive access it means that the file
        // is no longer locked by another process.
        try
        {
            using FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return inputStream.Length > 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error checking if {Filename} is readable", filename);
            return false;
        }
    }

    public async Task WaitForFile(string filename)
    {
        while (!IsFileReady(filename)) { await Task.Delay(100); }
    }

    public static async Task WaitWithTimeout(Task wait, string filename, TimeSpan timeout)
    {
        var timeoutTask = Task.Delay(2_000)!;
        var firstTask = await Task.WhenAny(wait, timeoutTask);
        if (!firstTask.IsCompletedSuccessfully)
            throw new FileLoadException("Error readonly file", filename, firstTask.Exception);
        if (firstTask == timeoutTask)
            throw new FileLoadException("Timed out waiting for file to be readable", filename);
    }

    private async Task SafeLoadModel(ModelDescription modelToSet, ModelParams parameters)
    {
        if (modelToSet.Filename == Path.GetFileName(parameters.ModelPath))
            return;
        await WaitWithTimeout(WaitForFile(modelToSet.Filename), modelToSet.Filename, TimeSpan.FromSeconds(20));
        var currentPerf = _performanceMonitor.GetPerformanceSummary();
        if (currentPerf.Ram.Available < modelToSet.SizeInMb)
            await _sessionCleanerService.DeleteSessionForRam(modelToSet.SizeInMb);
        _model = await LLamaWeights.LoadFromFileAsync(parameters);
    }

    public async Task LoadSelectedModel()
    {
        var modelToSet = await _modelCollection.GetSelectedModel();

        var parameters = new ModelParams(modelToSet.Filename)
        {
            ContextSize = 1024,
            Seed = (uint)Random.Shared.Next(1 << 30) << 2 | (uint)Random.Shared.Next(1 << 2),
            GpuLayerCount = 35,
        };

        try
        {
            var selectedFilename = modelToSet.Filename;
            if (_model is null || (_currentModel.Filename != selectedFilename))
            {
                await SafeLoadModel(modelToSet, parameters);
            }
        }
        catch (IOException ioException)
        {
            _logger.LogError(ioException, "{Model} already in use", Path.GetFileName(modelToSet.Filename));
            await SafeLoadModel(modelToSet, parameters);
        }
        catch (LLama.Exceptions.LoadWeightsFailedException e)
        {
            _logger.LogError(e, "Probably lacking memory trying to load {Model}", Path.GetFileName(e.ModelPath));
            await SafeLoadModel(modelToSet, parameters);
        }

        // Initialize a chat session
        _context = _model!.CreateContext(parameters);
        var executor = new InteractiveExecutor(_context);
        _chatSession = new ChatSession(executor);

        _currentModel = await _modelCollection.GetSelectedModel();
        if (_model is null || _chatSession is null || _context is null)
            throw new NoNullAllowedException();
    }

    public async Task Process(string prompt, int maxTokens, Func<string, Task> onNewText, CancellationToken cancellationToken)
    {
        var inferenceParams = new InferenceParams()
        {
            Temperature = 0.4f,
            MaxTokens = maxTokens,
            RepeatLastTokensCount = 0,
            RepeatPenalty = 1.2f,
        };

        var message = new ChatHistory.Message(AuthorRole.User, prompt);

        if (_chatSession!.History.Messages.LastOrDefault()?.AuthorRole == AuthorRole.User)
        {
            return;
        }

        var chats = _chatSession!.ChatAsync(message, inferenceParams, cancellationToken);

        await foreach (var text in chats)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("{Process} stopped. Cancellation was requested", nameof(Process));
                return;
            }

            await onNewText(text);
        }
    }

    public ValueTask DisposeAsync()
    {
        _context?.Dispose();
        _model?.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public async Task<List<ModelDescription>> GetModels()
    {
        return await _modelCollection.GetModels();
    }

    public Task SelectModel(string modelShortName)
    {
        _modelCollection.SelectModel(modelShortName);
        return Task.CompletedTask;
    }

    public Task<(string modelShortName, string[] conversation)> GetConversation()
    {
        var conversationContent = _chatSession.History.Messages.Select(s => s.Content).ToArray();
        return Task.FromResult((modelShortName: _currentModel.ShortName, conversation: conversationContent));
    }
}