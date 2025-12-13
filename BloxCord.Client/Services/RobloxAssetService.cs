using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BloxCord.Client.Services;

public static class RobloxAssetService
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<string?> ResolveDecalAsync(long assetId)
    {
        try
        {
            var url = $"https://thumbnails.roblox.com/v1/assets?assetIds={assetId}&returnPolicy=PlaceHolder&size=420x420&format=Png&isCircular=false";
            var response = await _httpClient.GetFromJsonAsync<ThumbnailResponse>(url);
            return response?.Data?.FirstOrDefault()?.ImageUrl;
        }
        catch
        {
            return null;
        }
    }

    private class ThumbnailResponse
    {
        [JsonPropertyName("data")]
        public List<ThumbnailData>? Data { get; set; }
    }

    private class ThumbnailData
    {
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
    }
}
