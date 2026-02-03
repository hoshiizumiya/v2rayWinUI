using System;
using System.IO;
using System.Threading.Tasks;
using DevWinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using ReactiveUI;
using ServiceLib.Common;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.Services;
using System.Reactive.Concurrency;
using Microsoft.UI.Dispatching;
using v2rayWinUI.Services;

namespace v2rayWinUI;

public partial class App : Application
{
    private Window? _window;

    internal static Window? StartupWindow { get; private set; }
    internal static bool NotifyIconCreated { get; set; }
    internal static IServiceProvider Services { get; private set; } = null!;
    internal Func<ServiceLib.Enums.EViewAction, object?, Task<bool>>? MainWindowHandler { get; private set; }


    public IThemeService ThemeService { get; set; }

    public App()
    {
        InitializeComponent();

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<IExceptionReporter, ExceptionReporter>();
        services.AddSingleton<IWindowStateService, WindowStateService>();
        Services = services.BuildServiceProvider();

        try
        {
            DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
        }
        catch { }

        try
        {
            IExceptionReporter reporter = Services.GetRequiredService<IExceptionReporter>();
            new Services.ExceptionHandlingService(this, reporter).Initialize();
        }
        catch { }

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

        // Single-instance: initialize before creating the main window
        SingleInstanceService singleInstance = new Services.SingleInstanceService();
        bool isFirst = singleInstance.Initialize(() =>
        {
            try
            {
                // If the main window is available, marshal activation to its DispatcherQueue
                if (StartupWindow is MainWindow mw2)
                {
                    try
                    {
                        mw2.DispatcherQueue?.TryEnqueue(() =>
                        {
                            try { mw2.Activate(); } catch { }
                        });
                    }
                    catch { }
                }
            }
            catch { }
        });

        if (!isFirst)
        {
            // Signal sent to existing instance; exit this process
            return;
        }

        _window = new MainWindow();
        new ModernSystemMenu(_window);
        WindowHelper.TrackWindow(_window);

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

    internal void SetMainWindowHandler(Func<ServiceLib.Enums.EViewAction, object?, Task<bool>> handler)
    {
        MainWindowHandler = handler;
    }
}
