using HomeChat.Backend.AIModels;

namespace HomeChat.Backend.Chats;

public record SessionInfo
{
    public required Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastActivity { get; set; }
    public required ModelDescription Model { get; set; }
}
