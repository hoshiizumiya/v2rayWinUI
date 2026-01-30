using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using System.Threading.Tasks;
using ServiceLib.Manager;
using ServiceLib.Models;
using WinRT.Interop;

namespace v2rayWinUI.Services;

public class WindowStateService
{
    private readonly Config _config;

    public WindowStateService()
    {
        _config = AppManager.Instance.Config;
    }

    private AppWindow GetAppWindow(Window window)
    {
        nint hWnd = WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    public void RestoreWindowSize(Window window)
    {
        try
        {
            var appWindow = GetAppWindow(window);
            var list = _config.UiItem?.WindowSizeItem;
            if (list != null)
            {
                var item = list.Find(t => t.TypeName == nameof(MainWindow));
                if (item != null && item.Width > 0 && item.Height > 0)
                {
                    appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = item.Width, Height = item.Height });
                }
            }
        }
        catch { }
    }

    public async Task SaveWindowSizeAsync(Window window)
    {
        try
        {
            var appWindow = GetAppWindow(window);
            var size = appWindow.Size;

            var list = _config.UiItem?.WindowSizeItem;
            if (list == null)
            {
                _config.UiItem = _config.UiItem ?? new UIItem();
                _config.UiItem.WindowSizeItem = new System.Collections.Generic.List<WindowSizeItem>();
                list = _config.UiItem.WindowSizeItem;
            }

            var item = list.Find(t => t.TypeName == nameof(MainWindow));
            if (item == null)
            {
                item = new WindowSizeItem { TypeName = nameof(MainWindow) };
                list.Add(item);
            }

            item.Width = size.Width;
            item.Height = size.Height;

            await ServiceLib.Handler.ConfigHandler.SaveConfig(_config);
        }
        catch { }
    }
}
