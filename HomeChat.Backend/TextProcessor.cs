using LLama;
using LLama.Common;

namespace HomeChat.Backend;

public interface ITextProcessor
{
    void Start();
    Task Process(string prompt, int maxTokens, Func<string, Task> onNewText, CancellationToken cancellationToken);
}

public class TextProcessor : ITextProcessor, IAsyncDisposable
{
    private readonly ModelParams _parameters;

    private readonly LLamaWeights _model;
    private ChatSession _session;
    private LLamaContext _context;
    private InteractiveExecutor _executor;
    public bool Started { get; private set; } = false;
    public TextProcessor(string modelPath)
    {
        var rand = new Random();
        _parameters = new ModelParams(modelPath)
        {
            ContextSize = 1024,
            Seed = (uint)(rand.Next(1 << 30)) << 2 | (uint)(rand.Next(1 << 2)),
            GpuLayerCount = 35,
        };
        _model = LLamaWeights.LoadFromFile(_parameters);
    }

    public void Start()
    {
        // Initialize a chat session
        _context = _model.CreateContext(_parameters);
        _executor = new InteractiveExecutor(_context);
        _session = new ChatSession(_executor);
        Started = true;
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
        var message = new ChatHistory.Message(AuthorRole.User, prompt);
        var chats = _session.ChatAsync(message, inferenceParams);

        await foreach (var text in chats)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Start();
                return;
            }

            await onNewText(text);
        }
    }

    public ValueTask DisposeAsync()
    {
        _context?.Dispose();
        _model?.Dispose();
        return ValueTask.CompletedTask;
    }
}