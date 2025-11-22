using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BloxCord.Client.Services;

internal static class RobloxAvatarDirectory
{
    private static readonly HttpClient ThumbnailsClient = RobloxHttpClientFactory.CreateThumbnailsClient();

    private static readonly ConcurrentDictionary<long, string> Cache = new();

    public static async Task<RobloxAvatarResolution?> TryResolveAsync(long? userId, string? username, CancellationToken cancellationToken = default)
    {
        long? resolvedUserId = userId;

        if (!resolvedUserId.HasValue && !string.IsNullOrWhiteSpace(username))
            resolvedUserId = await RobloxUsernameDirectory.TryResolveUserIdAsync(username, cancellationToken);

        if (!resolvedUserId.HasValue)
            return null;

        if (Cache.TryGetValue(resolvedUserId.Value, out var cached))
            return new RobloxAvatarResolution(resolvedUserId, cached);

        try
        {
            Console.WriteLine($"[AvatarDirectory] Fetching avatar for {resolvedUserId.Value}...");
            var response = await ThumbnailsClient.GetAsync($"/v1/users/avatar-headshot?userIds={resolvedUserId.Value}&size=150x150&format=Png&isCircular=false", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[AvatarDirectory] Failed to fetch avatar: {response.StatusCode}");
                return new RobloxAvatarResolution(resolvedUserId, string.Empty);
            }

            var payload = await response.Content.ReadFromJsonAsync<HeadshotResponse>(cancellationToken: cancellationToken);
            var imageUrl = payload?.Data?.FirstOrDefault()?.ImageUrl ?? string.Empty;

            Console.WriteLine($"[AvatarDirectory] Resolved avatar URL: {imageUrl}");

            if (!string.IsNullOrEmpty(imageUrl))
                Cache[resolvedUserId.Value] = imageUrl;

            return new RobloxAvatarResolution(resolvedUserId, imageUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AvatarDirectory] Exception fetching avatar: {ex.Message}");
            return new RobloxAvatarResolution(resolvedUserId, string.Empty);
        }
    }

    private sealed class HeadshotResponse
    {
        [JsonPropertyName("data")]
        public List<HeadshotEntry> Data { get; set; } = new();
    }

    private sealed class HeadshotEntry
    {
        [JsonPropertyName("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;
    }

    internal sealed record RobloxAvatarResolution(long? UserId, string AvatarUrl);
}
