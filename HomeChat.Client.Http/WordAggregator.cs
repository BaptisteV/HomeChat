namespace HomeChat.Client.Http;

public class WordAggregator
{
    private string _incompleteWord = "";
    private readonly Action<string> _onNewWord;

    public WordAggregator(Action<string> onNewWord)
    {
        _onNewWord = onNewWord;
    }

    private bool WordReady()
    {
        return _incompleteWord.Any(a => a == ',' || a == '.' || a == ';' || a == ':' || a == '!' || a == '?' || a == ' ');
    }

    public string Flush()
    {
        var remainingText = _incompleteWord;
        _incompleteWord = "";
        return remainingText;
    }

    public void NewText(string text)
    {
        _incompleteWord += text;
        if(WordReady())
        {
            _onNewWord(_incompleteWord);
            _incompleteWord = "";
        }
    }

}
