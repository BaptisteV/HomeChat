using HomeChat.Backend.AIModels;
using LLama;
using LLama.Common;
using System.Diagnostics.CodeAnalysis;

namespace HomeChat.Backend;

public interface ITextProcessor
{
    Task LoadSelectedModel();
    Task Process(string prompt, int maxTokens, Func<string, Task> onNewText, CancellationToken cancellationToken);
}

public class TextProcessor(IModelCollection _modelCollection) : ITextProcessor, IAsyncDisposable
{
    private LLamaWeights _model;
    private ChatSession _session;
    private LLamaContext _context;

    [MemberNotNull(nameof(_model))]
    [MemberNotNull(nameof(_session))]
    [MemberNotNull(nameof(_context))]
    public async Task LoadSelectedModel()
    {
        var rand = new Random();
        var modelFile = (await _modelCollection.GetSelectedModel()).Filename;
        var parameters = new ModelParams(modelFile)
        {
            ContextSize = 1024,
            Seed = (uint)rand.Next(1 << 30) << 2 | (uint)rand.Next(1 << 2),
            GpuLayerCount = 35,
        };
        _model = await LLamaWeights.LoadFromFileAsync(parameters)!;

        // Initialize a chat session
        _context = _model.CreateContext(parameters);
        var executor = new InteractiveExecutor(_context);
        _session = new ChatSession(executor);
    }

    public async Task Process(string prompt, int maxTokens, Func<string, Task> onNewText, CancellationToken cancellationToken)
    {
        var inferenceParams = new InferenceParams()
        {
            Temperature = 0.6f,
            MaxTokens = maxTokens,
            RepeatLastTokensCount = -1,
            RepeatPenalty = 1.2f,
        };

        if (_session is null)
            await LoadSelectedModel();

        var message = new ChatHistory.Message(AuthorRole.User, prompt);

        if (_session!.History.Messages.LastOrDefault()?.AuthorRole == AuthorRole.User)
        {
            return;
        }

        var chats = _session!.ChatAsync(message, inferenceParams, cancellationToken);

        await foreach (var text in chats)
        {
            if (cancellationToken.IsCancellationRequested)
            {
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
}