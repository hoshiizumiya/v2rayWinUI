using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.ViewModels;
using ServiceLib.Models;
using ServiceLib.Enums;
using ServiceLib.Common;
using ServiceLib.Handler;
using ServiceLib.Manager;

namespace v2rayWinUI.Views;

public sealed partial class AddServerWindow : Window
{
    private AddServerViewModel? ViewModel { get; set; }
    private ProfileItem ProfileItem { get; set; }

    public AddServerWindow(ProfileItem profileItem)
    {
        this.InitializeComponent();
        ProfileItem = profileItem;
        
        // Defer window-handle-dependent operations until window is shown.
        Activated += (s, e) =>
        {
            if (e.WindowActivationState != WindowActivationState.Deactivated)
            {
                // Only run once if possible
                Activated -= (s2, e2) => { }; // This is wrong syntax but you get the point
            }
        };

        InitializeViewModel();
        LoadData();
        SetupEventHandlers();
        
        // Instead of InitializeWindow() which uses hWnd, we'll let the caller or events handle it.
        UpdateTitle();
    }

    private void InitializeWindow()
    {
        // Set window size
        IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        
        appWindow.Resize(new Windows.Graphics.SizeInt32 
        { 
            Width = 1600, 
            Height = 1800 
        });
        
        // Update title based on server type
        UpdateTitle();
    }

    private void InitializeViewModel()
    {
        ViewModel = new AddServerViewModel(ProfileItem, null);
    }

    private void UpdateTitle()
    {
        string typeName = ProfileItem.ConfigType switch
        {
            EConfigType.VMess => "VMess",
            EConfigType.VLESS => "VLESS",
            EConfigType.Shadowsocks => "Shadowsocks",
            EConfigType.SOCKS => "SOCKS",
            EConfigType.HTTP => "HTTP",
            EConfigType.Trojan => "Trojan",
            EConfigType.Hysteria2 => "Hysteria2",
            EConfigType.TUIC => "TUIC",
            EConfigType.WireGuard => "WireGuard",
            _ => "Server"
        };
        
        this.Title = string.IsNullOrEmpty(ProfileItem.IndexId) 
            ? $"Add {typeName} Server" 
            : $"Edit {typeName} Server";
        
        txtServerType.Text = $"{typeName} Server Configuration";
    }

    private void LoadData()
    {
        if (ViewModel?.SelectedSource == null) return;
        
        ProfileItem server = ViewModel.SelectedSource;
        
        // Basic settings
        txtRemarks.Text = server.Remarks ?? string.Empty;
        txtAddress.Text = server.Address ?? string.Empty;
        txtPort.Text = server.Port.ToString();
        txtId.Text = server.Id ?? string.Empty;
        txtAlterId.Text = server.AlterId.ToString();
        
        // Transport settings
        txtRequestHost.Text = server.RequestHost ?? string.Empty;
        txtPath.Text = server.Path ?? string.Empty;
        
        // TLS settings
        txtAlpn.Text = server.Alpn ?? string.Empty;
        chkAllowInsecure.IsChecked = server.AllowInsecure == "true" || server.AllowInsecure == "1";
        
        // Advanced settings - removed TestUrl (doesn't exist)
        // Load combo box options
        LoadComboBoxes();
        
        // Set combo box selections
        SetComboBoxSelections(server);
        
        // Hide/Show controls based on server type
        UpdateControlVisibility();
    }

    private void LoadComboBoxes()
    {
        // Security options (varies by protocol)
        string[] securityOptions = ProfileItem.ConfigType switch
        {
            EConfigType.VMess => new[] { "auto", "aes-128-gcm", "chacha20-poly1305", "none" },
            EConfigType.VLESS => new[] { "none" },
            EConfigType.Shadowsocks => new[] { 
                "aes-128-gcm", "aes-256-gcm", "chacha20-ietf-poly1305",
                "2022-blake3-aes-128-gcm", "2022-blake3-aes-256-gcm", "2022-blake3-chacha20-poly1305"
            },
            _ => new[] { "auto", "none" }
        };
        cmbSecurity.ItemsSource = securityOptions;
        cmbSecurity.SelectedIndex = 0;
        
        // Network options
        cmbNetwork.ItemsSource = new[] { "tcp", "kcp", "ws", "http", "quic", "grpc", "httpupgrade", "splithttp" };
        cmbNetwork.SelectedIndex = 0;
        
        // Header type
        cmbHeaderType.ItemsSource = new[] { "none", "http", "srtp", "utp", "wechat-video", "dtls", "wireguard" };
        cmbHeaderType.SelectedIndex = 0;
        
        // Stream security
        cmbStreamSecurity.ItemsSource = new[] { "", "tls", "reality" };
        cmbStreamSecurity.SelectedIndex = 0;
        
        // Fingerprint
        cmbFingerprint.ItemsSource = new[] { "", "chrome", "firefox", "safari", "ios", "android", "edge", "360", "qq", "random", "randomized" };
        cmbFingerprint.SelectedIndex = 0;
        
        // Flow (for VLESS XTLS)
        cmbFlow.ItemsSource = new[] { "", "xtls-rprx-vision", "xtls-rprx-vision-udp443" };
        cmbFlow.SelectedIndex = 0;
    }

    private void SetComboBoxSelections(ProfileItem server)
    {
        // Set security
        if (!string.IsNullOrEmpty(server.Security))
        {
            int index = (cmbSecurity.ItemsSource as string[])?.ToList().IndexOf(server.Security) ?? -1;
            if (index >= 0) cmbSecurity.SelectedIndex = index;
        }
        
        // Set network
        if (!string.IsNullOrEmpty(server.Network))
        {
            int index = (cmbNetwork.ItemsSource as string[])?.ToList().IndexOf(server.Network) ?? -1;
            if (index >= 0) cmbNetwork.SelectedIndex = index;
        }
        
        // Set header type
        if (!string.IsNullOrEmpty(server.HeaderType))
        {
            int index = (cmbHeaderType.ItemsSource as string[])?.ToList().IndexOf(server.HeaderType) ?? -1;
            if (index >= 0) cmbHeaderType.SelectedIndex = index;
        }
        
        // Set stream security
        if (!string.IsNullOrEmpty(server.StreamSecurity))
        {
            int index = (cmbStreamSecurity.ItemsSource as string[])?.ToList().IndexOf(server.StreamSecurity) ?? -1;
            if (index >= 0) cmbStreamSecurity.SelectedIndex = index;
        }
        
        // Set fingerprint
        if (!string.IsNullOrEmpty(server.Fingerprint))
        {
            int index = (cmbFingerprint.ItemsSource as string[])?.ToList().IndexOf(server.Fingerprint) ?? -1;
            if (index >= 0) cmbFingerprint.SelectedIndex = index;
        }
        
        // Set flow
        if (!string.IsNullOrEmpty(server.Flow))
        {
            int index = (cmbFlow.ItemsSource as string[])?.ToList().IndexOf(server.Flow) ?? -1;
            if (index >= 0) cmbFlow.SelectedIndex = index;
        }
    }

    private void UpdateControlVisibility()
    {
        // Hide AlterID for non-VMess protocols
        panelAlterId.Visibility = ProfileItem.ConfigType == EConfigType.VMess 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    private void SetupEventHandlers()
    {
        // Generate ID button
        btnGenerateId.Click += (s, e) => GenerateId();
        
        // Import from URL
        btnImportUrl.Click += async (s, e) => await ImportFromUrl();
        
        // Test server
        btnTestServer.Click += async (s, e) => await TestServer();
        
        // Save button
        btnSave.Click += async (s, e) => await SaveServer();
        
        // Cancel button
        btnCancel.Click += (s, e) => this.Close();
        
        // Network changed - update relevant fields
        cmbNetwork.SelectionChanged += (s, e) => UpdateNetworkSettings();
    }

    private void GenerateId()
    {
        if (ProfileItem.ConfigType == EConfigType.VMess || 
            ProfileItem.ConfigType == EConfigType.VLESS)
        {
            txtId.Text = Guid.NewGuid().ToString();
        }
        else
        {
            txtId.Text = Utils.GetGuid(false);
        }
    }

    private async Task ImportFromUrl()
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = "Import from URL",
            PrimaryButtonText = "Import",
            CloseButtonText = "Cancel",
            XamlRoot = this.Content.XamlRoot
        };
        
        TextBox textBox = new TextBox 
        { 
            PlaceholderText = "Paste server URL here...",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 100
        };
        dialog.Content = textBox;
        
        ContentDialogResult result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            await ShowMessageAsync("Info", "URL import is not migrated yet. Use Ctrl+V in Servers page to import from clipboard.");
        }
    }

    private async Task TestServer()
    {
        // Save current data to temp profile
        SaveFormData();
        
        // TODO: Implement actual connection test
        await ShowMessageAsync("Test", "Connection test feature coming soon!");
    }

    private async Task SaveServer()
    {
        // Validate input
        if (!ValidateInput())
        {
            return;
        }
        
        // Save form data to ProfileItem
        SaveFormData();
        
        try
        {
            // Save to config (original behavior: update AppManager.Instance.Config)
            SaveFormData();
            Config config = AppManager.Instance.Config;
            int ret = await ConfigHandler.AddServer(config, ProfileItem);
            if (ret == 0)
            {
                await ShowMessageAsync("Success", "Server saved successfully!");
                this.Close();
            }
            else
            {
                await ShowMessageAsync("Error", "Failed to save server.");
            }
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Error", $"Error: {ex.Message}");
        }
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(txtRemarks.Text))
        {
            ShowMessageAsync("Validation", "Please enter a server name.").Wait();
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(txtAddress.Text))
        {
            ShowMessageAsync("Validation", "Please enter server address.").Wait();
            return false;
        }
        
        if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
        {
            ShowMessageAsync("Validation", "Please enter a valid port (1-65535).").Wait();
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(txtId.Text))
        {
            ShowMessageAsync("Validation", "Please enter User ID or generate one.").Wait();
            return false;
        }
        
        return true;
    }

    private void SaveFormData()
    {
        if (ViewModel?.SelectedSource == null) return;
        
        var server = ViewModel.SelectedSource;
        
        // Basic settings
        server.Remarks = txtRemarks.Text;
        server.Address = txtAddress.Text;
        server.Port = int.Parse(txtPort.Text);
        server.Id = txtId.Text;
        server.AlterId = int.TryParse(txtAlterId.Text, out int alterId) ? alterId : 0;
        server.Security = cmbSecurity.SelectedItem?.ToString() ?? "auto";
        
        // Transport settings
        server.Network = cmbNetwork.SelectedItem?.ToString() ?? "tcp";
        server.HeaderType = cmbHeaderType.SelectedItem?.ToString() ?? "none";
        server.RequestHost = txtRequestHost.Text;
        server.Path = txtPath.Text;
        
        // TLS settings - AllowInsecure is string (fixed)
        server.StreamSecurity = cmbStreamSecurity.SelectedItem?.ToString() ?? "";
        server.Alpn = txtAlpn.Text;
        server.Fingerprint = cmbFingerprint.SelectedItem?.ToString() ?? "";
        server.AllowInsecure = (chkAllowInsecure.IsChecked ?? false) ? "true" : "";
        
        // Advanced settings
        server.Flow = cmbFlow.SelectedItem?.ToString() ?? "";
        // Removed TestUrl assignment (property doesn't exist)
    }

    private void UpdateNetworkSettings()
    {
        // Enable/disable fields based on network type
        var network = cmbNetwork.SelectedItem?.ToString() ?? "tcp";
        
        // Path is used for ws, http, grpc, httpupgrade, splithttp
        txtPath.IsEnabled = network is "ws" or "http" or "grpc" or "httpupgrade" or "splithttp";
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.Content.XamlRoot
        };
        await dialog.ShowAsync();
    }
    
    private static Config? _config => ServiceLib.Manager.AppManager.Instance.Config;
}
