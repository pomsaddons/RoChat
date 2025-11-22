using System.Text.Json.Serialization;

namespace BloxCord.Client.Models;

public sealed class ChatMessageDto
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public long? UserId { get; set; }
        = null;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
        = DateTimeOffset.UtcNow;

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; } = string.Empty;
}

public sealed class ChannelParticipantDto
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public long? UserId { get; set; }
        = null;

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; } = string.Empty;
}

public sealed class ChannelSnapshotDto
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("history")]
    public List<ChatMessageDto>? History { get; set; }
        = new();

    [JsonPropertyName("participants")]
    public List<ChannelParticipantDto>? Participants { get; set; }
        = new();
}

public class ChannelJoinDto
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public long? UserId { get; set; }
        = null;
}

public sealed class PostChatMessageDto : ChannelJoinDto
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public sealed class TypingIndicatorDto
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("usernames")]
    public IReadOnlyCollection<string> Usernames { get; set; }
        = Array.Empty<string>();
}

public sealed class TypingNotificationDto
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("isTyping")]
    public bool IsTyping { get; set; }
        = false;
}
