using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.Common;
using ServiceLib.Handler;

namespace v2rayWinUI.Views;

public sealed partial class DNSSettingWindow : Window, Services.IDialogWindow
{
    private Config? _config;
    private TaskCompletionSource<bool>? _closeCompletionSource;
    private bool _dialogResult;

    public DNSSettingWindow()
    {
        this.InitializeComponent();
        _config = AppManager.Instance.Config;

        LoadSettings();
        SetupEventHandlers();

        Closed += (_, _) => CompleteDialogResult();
    }

    private void InitializeWindow()
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 600, Height = 500 });
    }

    private void LoadSettings()
    {
        // DNS settings are stored in RoutingBasicItem in this model
        if (_config?.RoutingBasicItem == null) return;

        chkUseSystemHosts.IsChecked = false; // Default
        txtNormalDNS.Text = string.Empty;
        txtTunnelDNS.Text = string.Empty;
    }

    private void SetupEventHandlers()
    {
        btnSave.Click += async (s, e) => await SaveSettings();
        btnCancel.Click += (s, e) => CloseWithResult(false);
    }

    private async Task SaveSettings()
    {
        if (_config?.RoutingBasicItem == null) return;

        try
        {
            await ConfigHandler.SaveConfig(_config);

            try
            {
                var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                string title = loader.GetString("v2rayWinUI.DNS.SaveSuccess.Title");
                string msg = loader.GetString("v2rayWinUI.DNS.SaveSuccess.Message");
                string ok = loader.GetString("v2rayWinUI.Common.OK");

                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = msg,
                    CloseButtonText = ok,
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch
            {
                var dialog = new ContentDialog
                {
                    Title = "Success",
                    Content = "DNS settings saved successfully!",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            CloseWithResult(true);
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"DNSSettingWindow error: {ex.Message}");
        }
    }

    public Task<bool> ShowDialogAsync(Window? owner, int width, int height)
    {
        _closeCompletionSource = new TaskCompletionSource<bool>();
        _dialogResult = false;

        if (owner != null)
        {
            Helpers.ModalWindowHelper.ShowModal(this, owner, width, height);
        }
        else
        {
            Activate();
        }

        return _closeCompletionSource.Task;
    }

    private void CloseWithResult(bool result)
    {
        _dialogResult = result;
        Close();
    }

    private void CompleteDialogResult()
    {
        _closeCompletionSource?.TrySetResult(_dialogResult);
    }
}
