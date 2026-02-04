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
        try
        {
            AppWindow appWindow = GetAppWindow(window);
            appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

            // Create dialog presenter first
            OverlappedPresenter presenter = OverlappedPresenter.CreateForDialog();

            // Set window owner BEFORE setting IsModal
            SetWindowOwner(appWindow, owner);

            // Now set modal flag
            presenter.IsModal = true;

            // Apply presenter to AppWindow
            appWindow.SetPresenter(presenter);

            // Center over owner
            CenterOverOwner(window, owner);

            // CRITICAL: Restore focus to owner when modal window closes
            window.Closed += (sender, args) =>
            {
                try
                {
                    // Reactivate the owner window as per reference code
                    owner?.Activate();
                }
                catch (Exception ex)
                {
                    ServiceLib.Common.Logging.SaveLog($"Failed to reactivate owner window: {ex.Message}");
                }
            };

            // Show the modal window
            appWindow.Show();
        }
        catch (Exception ex)
        {
            // Fallback: show as non-modal if modal fails
            ServiceLib.Common.Logging.SaveLog($"Failed to show modal window: {ex.Message}, showing as non-modal");
            try
            {
                window.Activate();
            }
            catch
            {
                // If even Activate fails, just throw
                throw;
            }
        }
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
