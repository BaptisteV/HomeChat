namespace HomeChat.Backend.Chats;

public interface IChatResponseWriter
{
    Task Write(string newText);
}
