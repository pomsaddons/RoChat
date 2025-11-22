namespace BloxCord.Client.Models;

public class ClientChatMessage
{
    public required string Username { get; init; }

    public required string Content { get; init; }

    public required DateTime Timestamp { get; init; }

    public required string JobId { get; init; }

    public long? UserId { get; init; }

    public string AvatarUrl { get; init; } = string.Empty;

    public bool IsSystemMessage { get; init; }
}
