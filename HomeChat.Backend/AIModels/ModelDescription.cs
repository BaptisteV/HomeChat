namespace HomeChat.Backend.AIModels;

public record ModelDescription
{
    public required string Filename { get; init; }
    public required string ShortName { get; init; }
    public required string Description { get; init; }
    public required long SizeInMb { get; init; }
    public required bool Selected { get; set; }
}
