using Microsoft.UI.Xaml;
using ServiceLib.Manager;
using ServiceLib.Common;
using ServiceLib.Services;
using ServiceLib.Models;
using System.IO;

namespace v2rayWinUI;

public partial class App : Application
{
    private Window? _window;

    internal static Window? StartupWindow { get; private set; }
    internal Func<ServiceLib.Enums.EViewAction, object?, Task<bool>>? MainWindowHandler { get; private set; }

    public App()
    {
        InitializeComponent();

        UnhandledException += (_, e) =>
        {
            try
            {
                Logging.SaveLog($"UnhandledException: {e.Exception?.GetType().FullName} {e.Exception?.Message}\n{e.Exception}");
            }
            catch { }
        };
        
        // Initialize ServiceLib components
        if (!AppManager.Instance.InitApp())
        {
            Logging.SaveLog("Failed to initialize app");
            Environment.Exit(0);
            return;
        }
        
        AppManager.Instance.InitComponents();

        try
        {
            Config config = AppManager.Instance.Config;
            string geoSitePath = Utils.GetBinPath("geosite.dat");
            string geoIpPath = Utils.GetBinPath("geoip.dat");

            if (!File.Exists(geoSitePath) || !File.Exists(geoIpPath))
            {
                UpdateService updateService = new UpdateService(config, async (_, __) => await Task.CompletedTask);
                _ = updateService.UpdateGeoFileAll();
            }
        }
        catch { }
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        StartupWindow = _window;
        if (_window is MainWindow mw)
        {
            MainWindowHandler = mw.UpdateViewHandler;
        }
        _window.Activate();
    }
}
