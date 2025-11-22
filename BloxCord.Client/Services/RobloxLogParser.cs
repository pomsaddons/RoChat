using System.IO;
using System.Text.RegularExpressions;
using BloxCord.Client.Models;

namespace BloxCord.Client.Services;

public static class RobloxLogParser
{
    // Patterns mirror Bloxstrap's ActivityWatcher (Integrations/ActivityWatcher.cs)
    private static readonly Regex GameJoiningEntryPattern =
        new(@"! Joining game '([0-9a-f\-]{36})' place ([0-9]+) at ([0-9\.]+)", RegexOptions.Compiled);

    private static readonly Regex GameJoiningUniversePattern =
        new(@"universeid:([0-9]+).*userid:([0-9]+)", RegexOptions.Compiled);

    public static async Task<RobloxSessionInfo?> TryReadLatestAsync(CancellationToken cancellationToken = default)
    {
        string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "logs");

        if (!Directory.Exists(logDirectory))
            return null;

        var directory = new DirectoryInfo(logDirectory);
        var latestLog = directory.GetFiles("*.log", SearchOption.TopDirectoryOnly)
            .Where(f => f.Name.Contains("Player", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latestLog is null)
            return null;

        var lines = await ReadAllLinesAsync(latestLog.FullName, cancellationToken);

        string? jobId = null;
        long? placeId = null;
        long? userId = null;

        for (int i = lines.Count - 1; i >= 0; i--)
        {
            string line = lines[i];

            if (jobId is null && line.Contains("! Joining game", StringComparison.Ordinal))
            {
                var match = GameJoiningEntryPattern.Match(line);

                if (match.Success)
                {
                    jobId = match.Groups[1].Value;
                    if (long.TryParse(match.Groups[2].Value, out var parsedPlace))
                        placeId = parsedPlace;
                }
            }

            if (userId is null && line.Contains("universeid:", StringComparison.Ordinal))
            {
                var match = GameJoiningUniversePattern.Match(line);

                if (match.Success && long.TryParse(match.Groups[2].Value, out var parsedUser))
                    userId = parsedUser;
            }

            if (jobId is not null && userId is not null)
                break;
        }

        if (string.IsNullOrWhiteSpace(jobId))
            return null;

        string username = string.Empty;

        if (userId is not null)
        {
            var user = await RobloxUserDirectory.TryGetUserAsync(userId.Value, cancellationToken);
            username = user?.Name ?? string.Empty;
        }

        return new RobloxSessionInfo
        {
            JobId = jobId,
            PlaceId = placeId,
            UserId = userId,
            Username = username
        };
    }

    private static async Task<List<string>> ReadAllLinesAsync(string path, CancellationToken cancellationToken)
    {
        var lines = new List<string>();

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lines.Add(await reader.ReadLineAsync() ?? string.Empty);
        }

        return lines;
    }
}
