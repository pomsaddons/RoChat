using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BloxCord.Client.Services;

internal static class RobloxUsernameDirectory
{
    private static readonly HttpClient UsersClient = RobloxHttpClientFactory.CreateUsersClient();

    public static async Task<long?> TryResolveUserIdAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        try
        {
            var request = new UsernameLookupRequest
            {
                Usernames = new[] { username },
                ExcludeBannedUsers = true
            };

            var response = await UsersClient.PostAsJsonAsync("/v1/usernames/users", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[UsernameDirectory] Failed to resolve username '{username}': {response.StatusCode}");
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<UsernameLookupResponse>(cancellationToken: cancellationToken);
            var id = payload?.Data?.FirstOrDefault()?.Id;
            
            Console.WriteLine($"[UsernameDirectory] Resolved '{username}' to ID: {id}");
            return id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UsernameDirectory] Exception resolving username '{username}': {ex.Message}");
            return null;
        }
    }

    private sealed class UsernameLookupRequest
    {
        [JsonPropertyName("usernames")]
        public IEnumerable<string> Usernames { get; set; } = Array.Empty<string>();

        [JsonPropertyName("excludeBannedUsers")]
        public bool ExcludeBannedUsers { get; set; }
            = true;
    }

    private sealed class UsernameLookupResponse
    {
        [JsonPropertyName("data")]
        public List<UsernameLookupEntry> Data { get; set; } = new();
    }

    private sealed class UsernameLookupEntry
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }
            = null;
    }
}
