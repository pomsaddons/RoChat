using System;
using DiscordRPC;
using DiscordRPC.Logging;

namespace BloxCord.Client.Services;

public class DiscordRpcService : IDisposable
{
    private DiscordRpcClient? _client;
    private const string ClientId = "1449246018515370025"; 

    public void Initialize()
    {
        if (_client != null) return;

        _client = new DiscordRpcClient(ClientId);
        
        // Set the logger
        _client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

        // Subscribe to events
        _client.OnReady += (sender, e) =>
        {
            Console.WriteLine($"Received Ready from user {e.User.Username}");
        };

        _client.OnPresenceUpdate += (sender, e) =>
        {
            Console.WriteLine($"Received Update! {e.Presence}");
        };

        _client.Initialize();
    }

    public void SetStatus(string details, string state, int? partySize = null, int? partyMax = null, DateTime? startTime = null)
    {
        if (_client == null || !_client.IsInitialized) return;

        var presence = new RichPresence()
        {
            Details = details,
            State = state,
            Assets = new Assets()
            {
                LargeImageKey = "rochat_logo", // Needs to be uploaded to Discord App assets
                LargeImageText = "RoChat",
                SmallImageKey = "roblox_icon",
                SmallImageText = "Roblox"
            }
        };

        if (startTime.HasValue)
        {
            presence.Timestamps = new Timestamps()
            {
                Start = startTime.Value
            };
        }

        if (partySize.HasValue && partyMax.HasValue)
        {
            presence.Party = new Party()
            {
                ID = Secrets.CreateFriendlySecret(new Random()),
                Size = partySize.Value,
                Max = partyMax.Value
            };
        }

        _client.SetPresence(presence);
    }

    public void ClearStatus()
    {
        _client?.ClearPresence();
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
