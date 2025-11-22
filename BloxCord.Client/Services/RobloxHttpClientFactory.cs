using System.Net;
using System.Net.Http;

namespace BloxCord.Client.Services;

internal static class RobloxHttpClientFactory
{
    private static HttpClient CreateClient(string baseAddress)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All
        };

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseAddress),
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("BloxstrapChat/1.0 (+https://github.com/)");
        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US");

        return client;
    }

    public static HttpClient CreateThumbnailsClient()
        => CreateClient("https://thumbnails.roblox.com");

    public static HttpClient CreateUsersClient()
        => CreateClient("https://users.roblox.com");
}
