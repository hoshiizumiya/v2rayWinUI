using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace v2rayWinUI.Helpers;

internal static class ModalWindowHelper
{
    public static void ShowModal(Window window, Window owner, int width, int height)
    {
        AppWindow appWindow = GetAppWindow(window);
        appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

        OverlappedPresenter presenter = OverlappedPresenter.CreateForDialog();
        presenter.IsModal = true;
        appWindow.SetPresenter(presenter);

        SetWindowOwner(appWindow, owner);
        CenterOverOwner(window, owner);
        appWindow.Show();
    }

    private static void CenterOverOwner(Window window, Window owner)
    {
        try
        {
            AppWindow ownedAppWindow = GetAppWindow(window);
            AppWindow ownerAppWindow = GetAppWindow(owner);
            Windows.Graphics.PointInt32 ownerPoint = ownerAppWindow.Position;
            Windows.Graphics.SizeInt32 ownerSize = ownerAppWindow.Size;
            Windows.Graphics.SizeInt32 ownedSize = ownedAppWindow.Size;

            int x = ownerPoint.X + ((ownerSize.Width - ownedSize.Width) / 2);
            int y = ownerPoint.Y + ((ownerSize.Height - ownedSize.Height) / 2);

            ownedAppWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }
        catch
        {
        }
    }

    private static AppWindow GetAppWindow(Window window)
    {
        nint hWnd = WindowNative.GetWindowHandle(window);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private static void SetWindowOwner(AppWindow owned, Window owner)
    {
        IntPtr ownerHwnd = WindowNative.GetWindowHandle(owner);
        IntPtr ownedHwnd = Win32Interop.GetWindowFromWindowId(owned.Id);

        if (IntPtr.Size == 8)
        {
            SetWindowLongPtr(ownedHwnd, -8, ownerHwnd);
        }
        else
        {
            SetWindowLong(ownedHwnd, -8, ownerHwnd);
        }
    }

    [DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLong")]
    public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
}
