using System.Configuration;
using System.Data;
using System.Windows;

namespace BloxCord.Client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool isTestMode = false;
        foreach (var arg in e.Args)
        {
            if (arg.Equals("--test", StringComparison.OrdinalIgnoreCase))
            {
                isTestMode = true;
            }
        }

        var mainWindow = new MainWindow();
        if (isTestMode)
        {
            mainWindow.EnableTestMode();
        }
        mainWindow.Show();
    }
}

