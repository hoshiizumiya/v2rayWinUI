using DevWinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using Sentry;
using ServiceLib.Common;
using ServiceLib.Events;
using ServiceLib.Handler;
using ServiceLib.Manager;
using ServiceLib.Models;
using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using v2rayWinUI.Services;
using v2rayWinUI.Views.Dialogs;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace v2rayWinUI;

public sealed partial class MainWindow : Window
{
    private readonly IDialogService _dialogService;
    private readonly IExceptionReporter _exceptionReporter;
    private readonly IWindowStateService _windowStateService;
    private TrayMenuService? _trayMenuService;
    private IntPtr _hwnd = IntPtr.Zero;
    private static IntPtr _oldWndProc = IntPtr.Zero;
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private static WndProcDelegate? _wndProcDelegateInstance;
    private volatile bool _isHandlingClose = false;

    public Views.MainView? MainViewInstance
    {
        get
        {
            try
            {
                return MainView;
            }
            catch { }

            return null;
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        _dialogService = new DialogService(TryGetXamlRoot);
        _exceptionReporter = App.Services.GetRequiredService<IExceptionReporter>();
        _windowStateService = App.Services.GetRequiredService<IWindowStateService>();

        // Set window title
        Title = $"v2rayWinUI - {ServiceLib.Common.Utils.GetVersion()}";

        InitializeWindow();
        // Restore saved window size if available
        try
        { _windowStateService.RestoreWindowSize(this); }
        catch { }
        InitializeTrayIcon();

        try
        {
            AppEvents.ShowHideWindowRequested
                .AsObservable()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(show =>
                {
                    if (show == null)
                    {
                        ToggleWindowVisibility();
                    }
                    else if (show == true)
                    {
                        ShowWindow();
                    }
                    else
                    {
                        HideWindow();
                    }
                });
        }
        catch { }

        try
        {
            AppEvents.AppExitRequested
                .AsObservable()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    try
                    {
                        SentrySdk.AddBreadcrumb("Application exiting via AppExitRequested", "lifecycle");
                        _trayMenuService?.Dispose();
                    }
                    catch { }

                    try
                    {
                        App.Current.Exit();
                    }
                    catch { }
                });
        }
        catch { }

        Closed += async (s, e) =>
        {
            _trayMenuService?.Dispose();
            try
            { await _windowStateService.SaveWindowSizeAsync(this); }
            catch { }
        };
    }

    internal FlyoutShowOptions GetFlyoutShowOptions()
    {
        return new FlyoutShowOptions
        {
            Placement = FlyoutPlacementMode.Auto,
            ShowMode = FlyoutShowMode.Standard,
        };
    }

    private void InitializeTrayIcon()
    {
        try
        {
            _trayMenuService = new TrayMenuService(this, _exceptionReporter);
            _trayMenuService.Initialize();
            App.NotifyIconCreated = true;
        }
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "MainWindow.InitializeTrayIcon");
        }
    }

    private void ToggleWindowVisibility()
    {
        try
        {
            if (Visible)
            {
                HideWindow();
            }
            else
            {
                ShowWindow();
            }
        }
        catch
        {

            try
            { ShowWindow(); }
            catch { }
        }
    }

    private void ShowWindow()
    {
        try
        {
            Activate();
        }
        catch { }
    }

    private XamlRoot? TryGetXamlRoot()
    {
        try
        {
            if (Content is FrameworkElement root)
            {
                return root.XamlRoot;
            }
        }
        catch { }

        return null;
    }

    private void HideWindow()
    {
        try
        {
            // WinUI Window does not have Hide(); best-effort minimize.
            AppWindow appWindow = GetAppWindow();
            appWindow.Hide();
        }
        catch { }
    }

    private AppWindow GetAppWindow()
    {
        nint hWnd = WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newProc);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int GWLP_WNDPROC = -4;
    private const uint WM_CLOSE = 0x0010;

    private void InitializeWindow()
    {
        // Window initialization
        nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        appWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;

        // Set default window size
        appWindow.Resize(new Windows.Graphics.SizeInt32
        {
            Width = 1800,
            Height = 1400
        });

        WindowManager manager = new WindowManager(this);
        manager.MinWidth = 1000;
        manager.MinHeight = 500;

        _windowStateService.RestoreWindowSize(this);

        // Subscribe to AppWindow close request to implement hide-to-tray or exit behavior
        try
        {
            // Install a Win32 window procedure to intercept WM_CLOSE because AppWindow.CloseRequested
            // is not available on all SDK versions. We enqueue a UI dialog and prevent immediate close.
            try
            {
                nint h = WindowNative.GetWindowHandle(this);
                _hwnd = (IntPtr)h;
                if (_hwnd != IntPtr.Zero)
                {
                    // keep delegate alive
                    _wndProcDelegateInstance = new WndProcDelegate(WndProc);
                    IntPtr newPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegateInstance);
                    _oldWndProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC, newPtr);
                }
            }
            catch { }
        }
        catch { }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (msg == WM_CLOSE)
            {
                if (_isHandlingClose)
                {
                    return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
                }

                _isHandlingClose = true;
                try
                {
                    this.DispatcherQueue?.TryEnqueue(async () =>
                    {
                        try
                        {
                            if (!App.NotifyIconCreated)
                            {
                                _isHandlingClose = false;
                                App.Current.Exit();
                                return;
                            }

                            Config cfg = AppManager.Instance.Config;
                            if (cfg.UiItem.Hide2TrayWhenCloseAsked)
                            {
                                if (cfg.UiItem.Hide2TrayWhenClose)
                                {
                                    HideWindow();
                                    _isHandlingClose = false;
                                    return;
                                }

                                _isHandlingClose = false;
                                App.Current.Exit();
                                return;
                            }

                            XamlRoot? dialogRoot = TryGetXamlRoot();
                            if (dialogRoot == null)
                            {
                                _isHandlingClose = false;
                                App.Current.Exit();
                                return;
                            }

                            CloseToTrayDialog dlg = new CloseToTrayDialog();
                            dlg.XamlRoot = dialogRoot;
                            ContentDialogResult res = await dlg.ShowAsync();
                            if (res == ContentDialogResult.Primary)
                            {
                                HideWindow();
                                if (dlg.RememberChoice)
                                {
                                    cfg.UiItem.Hide2TrayWhenClose = true;
                                    cfg.UiItem.Hide2TrayWhenCloseAsked = true;
                                    _ = ConfigHandler.SaveConfig(cfg);
                                }
                                _isHandlingClose = false;
                                return;
                            }

                            if (dlg.RememberChoice)
                            {
                                cfg.UiItem.Hide2TrayWhenClose = false;
                                cfg.UiItem.Hide2TrayWhenCloseAsked = true;
                                _ = ConfigHandler.SaveConfig(cfg);
                            }
                            _isHandlingClose = false;
                            App.Current.Exit();
                        }
                        catch
                        {
                            _isHandlingClose = false;
                        }
                    });
                }
                catch
                {
                    _isHandlingClose = false;
                }

                return IntPtr.Zero;
            }
        }
        catch { }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
