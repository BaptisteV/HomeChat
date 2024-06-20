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

public class Chat(IModelCollection _modelCollection, ILogger<Chat> _logger, IPerformanceMonitor performanceMonitor) : IChat
{
    private LLamaWeights _model;
    private ChatSession _chatSession;
    private LLamaContext _context;
    private ModelDescription _currentModel;
    private readonly IPerformanceMonitor _performanceMonitor = performanceMonitor;

    public async Task LoadSelectedModel()
    {
        var rand = new Random();

        var modelToSet = await _modelCollection.GetSelectedModel();
        var parameters = new ModelParams(modelToSet.Filename)
        {
            ContextSize = 1024,
            Seed = (uint)rand.Next(1 << 30) << 2 | (uint)rand.Next(1 << 2),
            GpuLayerCount = 35,
        };

        try
        {
            var selectedFilename = modelToSet.Filename;
            if (_model is null || (_currentModel.Filename != selectedFilename))
            {
                _model = await LLamaWeights.LoadFromFileAsync(parameters);
            }
        }
        catch (LLama.Exceptions.LoadWeightsFailedException e)
        {
            _logger.LogError(e, "Probably lacking memory trying to load {Model}", Path.GetFileName(e.ModelPath));
            await _performanceMonitor.DeleteInactiveSessions();
            throw;
        }

        // Initialize a chat session
        _context = _model.CreateContext(parameters);
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
                _logger.LogInformation("{Process} stopped. Cancellation was requested", nameof(Process));
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