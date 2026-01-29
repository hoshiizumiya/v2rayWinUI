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

namespace v2rayWinUI;

public sealed partial class MainWindow : Window
{
    private SystemTrayIcon? _systemTrayIcon;

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
        InitializeTrayIcon();

        Closed += (s, e) => _systemTrayIcon?.Dispose();
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
    }
}
