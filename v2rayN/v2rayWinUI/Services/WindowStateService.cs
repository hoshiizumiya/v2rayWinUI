using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using ServiceLib.Common;
using ServiceLib.Manager;
using ServiceLib.Models;
using System.Threading.Tasks;
using WinRT.Interop;

namespace v2rayWinUI.Services;

internal interface IWindowStateService
{
    public void RestoreWindowSize(Window window);
    public Task SaveWindowSizeAsync(Window window);
}

internal sealed class WindowStateService : IWindowStateService
{
    private readonly Config _config;

    public WindowStateService()
    {
        _config = AppManager.Instance.Config;
    }

    private AppWindow GetAppWindow(Window window)
    {
        nint hWnd = WindowNative.GetWindowHandle(window);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    public void RestoreWindowSize(Window window)
    {
        try
        {
            AppWindow appWindow = GetAppWindow(window);
            System.Collections.Generic.List<WindowSizeItem>? list = _config.UiItem?.WindowSizeItem;
            if (list != null)
            {
                WindowSizeItem? item = list.Find(t => t.TypeName == nameof(MainWindow));
                if (item != null && item.Width > 0 && item.Height > 0)
                {
                    appWindow.Resize(
                        new Windows.Graphics.SizeInt32
                        {
                            Width = item.Width,
                            Height = item.Height
                        }
                        );
                }
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog("WindowStateService.RestoreWindowSize", ex);
        }
    }

    public async Task SaveWindowSizeAsync(Window window)
    {
        try
        {
            AppWindow appWindow = GetAppWindow(window);
            Windows.Graphics.SizeInt32 size = appWindow.Size;

            System.Collections.Generic.List<WindowSizeItem>? list = _config.UiItem?.WindowSizeItem;
            if (list == null)
            {
                _config.UiItem = _config.UiItem ?? new UIItem();
                _config.UiItem.WindowSizeItem = new System.Collections.Generic.List<WindowSizeItem>();
                list = _config.UiItem.WindowSizeItem;
            }

            WindowSizeItem? item = list.Find(t => t.TypeName == nameof(MainWindow));
            if (item == null)
            {
                item = new WindowSizeItem { TypeName = nameof(MainWindow) };
                list.Add(item);
            }

            item.Width = size.Width;
            item.Height = size.Height;

            await ServiceLib.Handler.ConfigHandler.SaveConfig(_config);
        }
        catch (Exception ex)
        {
            Logging.SaveLog("WindowStateService.SaveWindowSizeAsync", ex);
        }
    }
}
