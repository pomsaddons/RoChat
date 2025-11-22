using System.Net.Http;
using System.Net.Http.Json;

namespace BloxCord.Client.Services;

internal static class RobloxUserDirectory
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("https://users.roblox.com")
    };

    public static async Task<RobloxUserRecord?> TryGetUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.GetAsync($"/v1/users/{userId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var payload = await response.Content.ReadFromJsonAsync<RobloxUserResponse>(cancellationToken: cancellationToken);
            if (payload is null)
                return null;

            return new RobloxUserRecord
            {
                Id = payload.Id,
                Name = payload.Name
            };
        }
        catch
        {
            return null;
        }
    }

    private sealed class RobloxUserResponse
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class RobloxUserRecord
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
