using System.IO;
using System.Text.Json;

namespace BloxCord.Client.Services;

public class AppConfig
{
    public string BackendUrl { get; set; } = "https://rochat.pompompurin.tech";
    public string Username { get; set; } = string.Empty;
    public bool UseGradient { get; set; } = true;
    public string SolidColor { get; set; } = "#020617";
    public string GradientStart { get; set; } = "#050505";
    public string GradientEnd { get; set; } = "#E5E5E5";
}

public static class ConfigService
{
    private const string ConfigFileName = "config.json";
    private static AppConfig _currentConfig = new();

    public static AppConfig Current => _currentConfig;

    public static void Load()
    {
        try
        {
            if (File.Exists(ConfigFileName))
            {
                var json = File.ReadAllText(ConfigFileName);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                {
                    _currentConfig = config;
                }
            }
        }
        catch
        {
            // Ignore errors, use defaults
        }
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_currentConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFileName, json);
        }
        catch
        {
            // Ignore errors
        }
    }
}
