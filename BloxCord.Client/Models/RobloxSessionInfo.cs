namespace BloxCord.Client.Models;

public sealed class RobloxSessionInfo
{
    public string JobId { get; init; } = string.Empty;

    public long? UserId { get; init; }
        = null;

    public long? PlaceId { get; init; }
        = null;

    public string Username { get; init; } = string.Empty;
}
