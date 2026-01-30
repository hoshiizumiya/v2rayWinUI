using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using ServiceLib.Common;
using ServiceLib.Manager;
using WinRT.Interop;
using System;
using DevWinUI;
using v2rayWinUI.Views.Tray;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI;
using v2rayWinUI.Services;
using ServiceLib.Handler;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Manager;
using System.Runtime.InteropServices;
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using v2rayWinUI.Views.Dialogs;

namespace v2rayWinUI;

public sealed partial class MainWindow : Window
{
    private SystemTrayIcon? _systemTrayIcon;
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

        // Set window title
        Title = $"v2rayWinUI - {ServiceLib.Common.Utils.GetVersion()}";

        InitializeWindow();
        // Restore saved window size if available
        try { new WindowStateService().RestoreWindowSize(this); } catch { }
        InitializeTrayIcon();

        Closed += async (s, e) =>
        {
            _systemTrayIcon?.Dispose();
            try { await new WindowStateService().SaveWindowSizeAsync(this); } catch { }
        };
    }

    private void InitializeTrayIcon()
    {
        try
        {
            uint trayId = 1;
            // WindowHelper.GetWindowIcon returns a Microsoft.UI.IconId in this environment.
            IconId iconId = WindowHelper.GetWindowIcon(this);

            _systemTrayIcon = new SystemTrayIcon(trayId, iconId, Title);
            _systemTrayIcon.IsVisible = true;

            _systemTrayIcon.LeftClick += (s, e) => ShowWindow();
            _systemTrayIcon.RightClick += (s, e) =>
            {
                TrayMenuFlyout trayMenu = new TrayMenuFlyout();
                trayMenu.ShowRequested += (s2, e2) => ShowWindow();
                trayMenu.ExitRequested += (s2, e2) => App.Current.Exit();
                trayMenu.ProxyModeChanged += (s2, mode) =>
                {
                    if (MainViewInstance?._statusBarViewModel != null)
                    {
                        MainViewInstance._statusBarViewModel.SystemProxySelected = mode;
                    }
                };
                trayMenu.UpdateSubsRequested += (s2, e2) =>
                {
                    if (MainViewInstance?._statusBarViewModel != null)
                    {
                        MainViewInstance._statusBarViewModel.SubUpdateCmd.Execute().Subscribe();
                    }
                };

                e.Flyout = trayMenu;
            };
        }
        catch { }
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
        manager.MinWidth = 2000;
        manager.MinHeight = 1000;

        // Attempt to restore saved window size (if any)
        try { new WindowStateService().RestoreWindowSize(this); } catch { }

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
                // If already handling, pass through to old proc
                if (_isHandlingClose)
                {
                    return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
                }

                // Prevent immediate close and show dialog on UI thread
                _isHandlingClose = true;
                try
                {
                    // Use dispatcher to show dialog asynchronously
                    this.DispatcherQueue?.TryEnqueue(async () =>
                    {
                        try
                        {
                            var cfg = AppManager.Instance.Config;
                            if (cfg.UiItem.Hide2TrayWhenCloseAsked)
                            {
                                if (cfg.UiItem.Hide2TrayWhenClose)
                                {
                                    // hide to tray
                                    HideWindow();
                                    _isHandlingClose = false;
                                    return;
                                }
                                else
                                {
                                    // allow close
                                    _isHandlingClose = true;
                                    PostClose();
                                    return;
                                }
                            }

                            var dlg = new CloseToTrayDialog();
                            dlg.XamlRoot = this.Content.XamlRoot;
                            var res = await dlg.ShowAsync();
                            if (res == ContentDialogResult.Primary)
                            {
                                // hide to tray
                                HideWindow();
                                if (dlg.RememberChoice)
                                {
                                    cfg.UiItem.Hide2TrayWhenClose = true;
                                    cfg.UiItem.Hide2TrayWhenCloseAsked = true;
                                    _ = ConfigHandler.SaveConfig(cfg);
                                }
                                _isHandlingClose = false;
                            }
                            else
                            {
                                if (dlg.RememberChoice)
                                {
                                    cfg.UiItem.Hide2TrayWhenClose = false;
                                    cfg.UiItem.Hide2TrayWhenCloseAsked = true;
                                    _ = ConfigHandler.SaveConfig(cfg);
                                }
                                // proceed to close
                                PostClose();
                            }
                        }
                        catch { _isHandlingClose = false; }
                    });
                }
                catch { _isHandlingClose = false; }

                // return 0 to indicate message handled
                return IntPtr.Zero;
            }
        }
        catch { }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private void PostClose()
    {
        // post WM_CLOSE back to window to continue closing
        try
        {
            if (_hwnd != IntPtr.Zero)
            {
                _ = Task.Run(() =>
                {
                    Thread.Sleep(10);
                    PostMessage(_hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                });
            }
        }
        catch { }
    }

    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
