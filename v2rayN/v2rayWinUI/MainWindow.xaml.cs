using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using DevWinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using ReactiveUI;
using Sentry;
using ServiceLib.Common;
using ServiceLib.Events;
using ServiceLib.Handler;
using ServiceLib.Manager;
using ServiceLib.Models;
using v2rayWinUI.Services;
using v2rayWinUI.Views.Dialogs;
using WinRT.Interop;

namespace v2rayWinUI;

public sealed partial class MainWindow : Window
{
    private readonly IDialogService _dialogService;
    private readonly IWindowStateService _windowStateService;
    private readonly CompositeDisposable _disposables = new();

    private TrayMenuService? _trayMenuService;
    private AppWindow? _appWindow;
    private bool _isHandleCloseLogicRunning = false;

    public Views.MainView? MainViewInstance => Content as Views.MainView;

    public MainWindow()
    {
        InitializeComponent();

        // 依赖注入获取服务
        _dialogService = new DialogService(TryGetXamlRoot);
        _windowStateService = App.Services.GetRequiredService<IWindowStateService>();

        Title = $"v2rayWinUI - {ServiceLib.Common.Utils.GetVersion()}";

        InitializeWindow();
        InitializeTrayIcon();
        SetupEventSubscriptions();

        // 窗口关闭时的清理
        Closed += (s, e) =>
        {
            _disposables.Dispose();
            _trayMenuService?.Dispose();
            // 尝试保存窗口状态，不阻塞
            _ = _windowStateService.SaveWindowSizeAsync(this);
        };
    }

    private void InitializeWindow()
    {
        // 获取 WinUI 3 原生 AppWindow 实例
        nint hWnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // 设置标题栏
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            _appWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }

        // 恢复窗口尺寸
        _appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1800, Height = 1400 });
        _windowStateService.RestoreWindowSize(this);

        // 设置最小尺寸限制
        WindowManager manager = new WindowManager(this)
        {
            MinWidth = 1000,
            MinHeight = 500
        };

        // 使用 AppWindow.Closing 代替 WndProc 钩子
        _appWindow.Closing += AppWindow_Closing;
    }

    private void SetupEventSubscriptions()
    {
        // 处理显隐请求
        AppEvents.ShowHideWindowRequested
            .AsObservable()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(show =>
            {
                if (show == null)
                    ToggleWindowVisibility();
                else if (show == true)
                    ShowWindow();
                else
                    HideWindow();
            })
            .DisposeWith(_disposables);

        // 处理退出请求
        AppEvents.AppExitRequested
            .AsObservable()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => HandleAppExitRequest())
            .DisposeWith(_disposables);
    }

    private void InitializeTrayIcon()
    {
        try
        {
            _trayMenuService = new TrayMenuService(this);
            _trayMenuService.Initialize();
            App.NotifyIconCreated = true;
        }
        catch (Exception ex)
        {
            ServiceLib.Common.Logging.SaveLog("MainWindow.InitializeTrayIcon", ex);
        }
    }

    /// <summary>
    /// 核心逻辑：拦截窗口关闭事件
    /// </summary>
    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // 1. 如果没有创建托盘图标，直接退出
        if (!App.NotifyIconCreated)
        {
            return;
        }

        // 2. 取消本次关闭，改为手动接管逻辑
        args.Cancel = true;

        // 防止重入
        if (_isHandleCloseLogicRunning)
            return;
        _isHandleCloseLogicRunning = true;

        try
        {
            await HandleCloseStrategyAsync();
        }
        catch (Exception ex)
        {
            ServiceLib.Common.Logging.SaveLog("MainWindow.CloseHandling", ex);
            // 出错兜底：强制退出
            ForceExit();
        }
        finally
        {
            _isHandleCloseLogicRunning = false;
        }
    }

    private async Task HandleCloseStrategyAsync()
    {
        Config cfg = AppManager.Instance.Config;

        // 策略A: 用户已设置直接退出或隐藏，不再询问
        if (cfg.UiItem.Hide2TrayWhenCloseAsked)
        {
            if (cfg.UiItem.Hide2TrayWhenClose)
            {
                HideWindow();
            }
            else
            {
                ForceExit();
            }
            return;
        }

        // 策略B: 弹窗询问用户
        XamlRoot? dialogRoot = TryGetXamlRoot();
        if (dialogRoot == null)
        {
            ForceExit();
            return;
        }

        CloseToTrayDialog dlg = new CloseToTrayDialog { XamlRoot = dialogRoot };
        ContentDialogResult res = await dlg.ShowAsync();

        // 更新用户配置
        if (dlg.RememberChoice)
        {
            cfg.UiItem.Hide2TrayWhenCloseAsked = true;
            cfg.UiItem.Hide2TrayWhenClose = (res == ContentDialogResult.Primary);
            _ = ConfigHandler.SaveConfig(cfg);
        }

        if (res == ContentDialogResult.Primary)
        {
            HideWindow();
        }
        else
        {
            ForceExit();
        }
    }

    private void HandleAppExitRequest()
    {
        SentrySdk.AddBreadcrumb("Application exiting via AppExitRequested", "lifecycle");
        _trayMenuService?.Dispose();
        ForceExit();
    }

    private void ForceExit()
    {
        // 移除 Closing 事件监听，防止死循环
        if (_appWindow != null)
        {
            _appWindow.Closing -= AppWindow_Closing;
        }
        App.Current.Exit();
    }

    #region Window Visibility Helpers

    private void ToggleWindowVisibility()
    {
        if (Visible)
            HideWindow();
        else
            ShowWindow();
    }

    private void ShowWindow()
    {
        try
        {
            // 确保窗口还原（如果被最小化）
            if (_appWindow != null)
            {
                _appWindow.Show();
            }
            // 激活窗口到前台
            Activate();
        }
        catch (Exception ex)
        {
            ServiceLib.Common.Logging.SaveLog("MainWindow.ShowWindow", ex);
        }
    }

    private void HideWindow()
    {
        try
        {
            _appWindow?.Hide();
        }
        catch (Exception ex)
        {
            ServiceLib.Common.Logging.SaveLog("MainWindow.HideWindow", ex);
        }
    }

    #endregion

    #region Helpers

    internal FlyoutShowOptions GetFlyoutShowOptions()
    {
        return new FlyoutShowOptions
        {
            Placement = FlyoutPlacementMode.Auto,
            ShowMode = FlyoutShowMode.Standard,
        };
    }

    private XamlRoot? TryGetXamlRoot()
    {
        if (Content is FrameworkElement root && root.XamlRoot != null)
        {
            return root.XamlRoot;
        }
        // 如果 Content 获取失败，尝试兜底获取
        return this.Content?.XamlRoot;
    }

    #endregion
}
