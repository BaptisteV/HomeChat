using LLama;
using LLama.Common;
using System.Diagnostics;
using System.Text;

namespace HomeChat.AI;

public interface ITextProcessor
{
    void Start();
    Task<string> Process(string prompt, Action<string> onNewText, Func<Task?, string, bool> emitText);
}

public class TextProcessor : ITextProcessor
{
    private const string _modelPath = @"C:\Users\Bapt\.cache\lm-studio\models\TheBloke\Llama-2-13B-chat-GGUF\llama-2-13b-chat.Q5_K_M.gguf";

    private readonly ModelParams _parameters;
    private readonly InferenceParams _inferenceParams;

    private readonly LLamaWeights _model;
    private ChatSession _session;
    private LLamaContext _context;
    private InteractiveExecutor _executor;
    public TextProcessor()
    {
        var rand = new Random();

        _parameters = new ModelParams(_modelPath)
        {
            ContextSize = 1200,
            Seed = (uint)(rand.Next(1 << 30)) << 2 | (uint)(rand.Next(1 << 2)),
            GpuLayerCount = 35,
        };
        _model = LLamaWeights.LoadFromFile(_parameters);

        _inferenceParams = new InferenceParams()
        {
            Temperature = 0.6f,
            MaxTokens = 500,
            RepeatLastTokensCount = -1,
            RepeatPenalty = 1.2f,
        };
    }

    public void Start()
    {
        // Initialize a chat session
        _context = _model.CreateContext(_parameters);
        _executor = new InteractiveExecutor(_context);
        _session = new ChatSession(_executor);
    }
    
    public async Task<string> Process(string prompt, Action<string> onNewText, Func<Task?, string, bool> emitText)
    {
        var chats = _session.ChatAsync(prompt, _inferenceParams);

        Task? speakerTask = null;
        var nextSpeech = "";
        var result = new StringBuilder();
        await foreach (var text in chats)
        {
            Console.Write(text);

            if (emitText(speakerTask, nextSpeech))
            {
                onNewText(nextSpeech);
                nextSpeech = "";
            }
            nextSpeech += text;

            result.Append(text);
        }
        return result.ToString();
    }
}