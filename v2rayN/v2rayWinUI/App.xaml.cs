using System;
using System.IO;
using System.Threading.Tasks;
using DevWinUI;
using Microsoft.UI.Xaml;
using ReactiveUI;
using ServiceLib.Common;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.Services;
using System.Reactive.Concurrency;

namespace v2rayWinUI;

public partial class App : Application
{
    private Window? _window;

    internal static Window? StartupWindow { get; private set; }
    internal Func<ServiceLib.Enums.EViewAction, object?, Task<bool>>? MainWindowHandler { get; private set; }


    public IThemeService ThemeService { get; set; }

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

        Logging.Setup();

        AppManager.Instance.InitComponents();

        try
        {
            AppManager.Instance.ShowInTaskbar = true;
        }
        catch { }

        try
        {
            Config config = AppManager.Instance.Config;
            string geoSitePath = ServiceLib.Common.Utils.GetBinPath("geosite.dat");
            string geoIpPath = ServiceLib.Common.Utils.GetBinPath("geoip.dat");

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
        try
        {
            RxApp.MainThreadScheduler = DispatcherQueueScheduler.Current;
        }
        catch { }

        _window = new MainWindow();

        try
        {
            Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue = _window.DispatcherQueue;
            RxApp.MainThreadScheduler = new DispatcherQueueScheduler(dispatcherQueue);
        }
        catch { }

        ThemeService = new ThemeService();
        // get app appdata local path?
        ThemeService.ConfigureAutoSave(true).Initialize(_window);

        StartupWindow = _window;
        if (_window is MainWindow mw)
        {
            try
            {
                if (mw.MainViewInstance != null)
                {
                    MainWindowHandler = mw.MainViewInstance.UpdateViewHandler;
                }
                else
                {
                    MainWindowHandler = null;
                }
            }
            catch
            {
                MainWindowHandler = null;
            }
        }
        _window.Activate();
    }
}
