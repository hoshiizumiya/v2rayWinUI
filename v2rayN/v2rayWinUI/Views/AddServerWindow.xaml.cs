using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using ReactiveUI;
using ServiceLib;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.ViewModels;
using v2rayWinUI.Helpers;

namespace v2rayWinUI.Views;

public sealed partial class AddServerWindow : Window, Services.IDialogWindow
{
    public AddServerViewModel? ViewModel { get; set; }
    private readonly ProfileItem _profileItem;
    private TaskCompletionSource<bool>? _closeCompletionSource;
    private bool _dialogResult;
    private bool _isUpdatingUI;

    public AddServerWindow(ProfileItem profileItem)
    {
        InitializeComponent();
        _profileItem = profileItem;

        ViewModel = new AddServerViewModel(_profileItem, UpdateViewHandler);

        InitializeComboBoxes();
        SetupEventHandlers();
        UpdateVisibility();
        LoadData();

        ThemeService themeService = new ThemeService();
        themeService.Initialize(this);

        Title = $"{_profileItem.ConfigType}";
        Closed += (_, _) => CompleteDialogResult();
    }

    private void InitializeComboBoxes()
    {
        cmbCoreType.ItemsSource = Global.CoreTypes.AppendEmpty();
        cmbNetwork.ItemsSource = Global.Networks;
        cmbFingerprint.ItemsSource = Global.Fingerprints;
        cmbFingerprint2.ItemsSource = Global.Fingerprints;
        cmbAllowInsecure.ItemsSource = Global.AllowInsecure;
        cmbAlpn.ItemsSource = Global.Alpns;
        cmbEchForceQuery.ItemsSource = Global.EchForceQuerys;

        List<string> lstStreamSecurity = new List<string> { string.Empty, Global.StreamSecurity };

        switch (_profileItem.ConfigType)
        {
            case EConfigType.VMess:
                gridVMess.Visibility = Visibility.Visible;
                cmbSecurity.ItemsSource = Global.VmessSecurities;
                if (_profileItem.Security.IsNullOrEmpty())
                {
                    _profileItem.Security = Global.DefaultSecurity;
                }
                break;

            case EConfigType.Shadowsocks:
                gridSs.Visibility = Visibility.Visible;
                cmbSecurity3.ItemsSource = AppManager.Instance.GetShadowsocksSecurities(_profileItem);
                break;

            case EConfigType.SOCKS:
            case EConfigType.HTTP:
                gridSocks.Visibility = Visibility.Visible;
                break;

            case EConfigType.VLESS:
                gridVLESS.Visibility = Visibility.Visible;
                lstStreamSecurity.Add(Global.StreamSecurityReality);
                cmbFlow5.ItemsSource = Global.Flows;
                if (_profileItem.Security.IsNullOrEmpty())
                {
                    _profileItem.Security = Global.None;
                }
                break;

            case EConfigType.Trojan:
                gridTrojan.Visibility = Visibility.Visible;
                lstStreamSecurity.Add(Global.StreamSecurityReality);
                cmbFlow6.ItemsSource = Global.Flows;
                break;

            case EConfigType.Hysteria2:
                gridHysteria2.Visibility = Visibility.Visible;
                gridTransport.Visibility = Visibility.Collapsed;
                cmbFingerprint.IsEnabled = false;
                cmbFingerprint.Text = string.Empty;
                break;

            case EConfigType.TUIC:
                gridTuic.Visibility = Visibility.Visible;
                gridTransport.Visibility = Visibility.Collapsed;
                cmbCoreType.IsEnabled = false;
                cmbFingerprint.IsEnabled = false;
                cmbFingerprint.Text = string.Empty;
                cmbHeaderType8.ItemsSource = Global.TuicCongestionControls;
                break;

            case EConfigType.WireGuard:
                gridWireguard.Visibility = Visibility.Visible;
                gridTransport.Visibility = Visibility.Collapsed;
                gridTls.Visibility = Visibility.Collapsed;
                break;

            case EConfigType.Anytls:
                gridAnytls.Visibility = Visibility.Visible;
                cmbCoreType.IsEnabled = false;
                lstStreamSecurity.Add(Global.StreamSecurityReality);
                break;
        }

        cmbStreamSecurity.ItemsSource = lstStreamSecurity;
        gridTlsMore.Visibility = Visibility.Collapsed;
        gridRealityMore.Visibility = Visibility.Collapsed;
    }

    private void LoadData()
    {
        if (ViewModel?.SelectedSource == null) return;

        _isUpdatingUI = true;

        ProfileItem source = ViewModel.SelectedSource;

        // Basic settings
        cmbCoreType.Text = ViewModel.CoreType ?? string.Empty;
        txtRemarks.Text = source.Remarks ?? string.Empty;
        txtAddress.Text = source.Address ?? string.Empty;
        txtPort.Text = source.Port.ToString();

        // Protocol-specific
        switch (_profileItem.ConfigType)
        {
            case EConfigType.VMess:
                txtId.Text = source.Id ?? string.Empty;
                txtAlterId.Text = source.AlterId.ToString();
                cmbSecurity.Text = source.Security ?? Global.DefaultSecurity;
                togmuxEnabled.IsChecked = source.MuxEnabled;
                break;

            case EConfigType.Shadowsocks:
                txtId3.Text = source.Id ?? string.Empty;
                cmbSecurity3.Text = source.Security ?? Global.DefaultSecurity;
                togmuxEnabled3.IsChecked = source.MuxEnabled;
                break;

            case EConfigType.SOCKS:
            case EConfigType.HTTP:
                txtId4.Text = source.Id ?? string.Empty;
                txtSecurity4.Text = source.Security ?? string.Empty;
                break;

            case EConfigType.VLESS:
                txtId5.Text = source.Id ?? string.Empty;
                cmbFlow5.Text = source.Flow ?? string.Empty;
                txtSecurity5.Text = source.Security ?? Global.None;
                togmuxEnabled5.IsChecked = source.MuxEnabled;
                break;

            case EConfigType.Trojan:
                txtId6.Text = source.Id ?? string.Empty;
                cmbFlow6.Text = source.Flow ?? string.Empty;
                togmuxEnabled6.IsChecked = source.MuxEnabled;
                break;

            case EConfigType.Hysteria2:
                txtId7.Text = source.Id ?? string.Empty;
                txtPath7.Text = source.Path ?? string.Empty;
                txtPorts7.Text = source.Ports ?? string.Empty;
                break;

            case EConfigType.TUIC:
                txtId8.Text = source.Id ?? string.Empty;
                txtSecurity8.Text = source.Security ?? string.Empty;
                cmbHeaderType8.Text = source.HeaderType ?? string.Empty;
                break;

            case EConfigType.WireGuard:
                txtId9.Text = source.Id ?? string.Empty;
                txtPublicKey9.Text = source.PublicKey ?? string.Empty;
                txtPath9.Text = source.Path ?? string.Empty;
                txtRequestHost9.Text = source.RequestHost ?? string.Empty;
                txtShortId9.Text = source.ShortId ?? string.Empty;
                break;

            case EConfigType.Anytls:
                txtId10.Text = source.Id ?? string.Empty;
                break;
        }

        // Transport settings
        cmbNetwork.Text = source.Network ?? Global.DefaultNetwork;
        cmbHeaderType.Text = source.HeaderType ?? Global.None;
        txtRequestHost.Text = source.RequestHost ?? string.Empty;
        txtPath.Text = source.Path ?? string.Empty;
        txtExtra.Text = source.Extra ?? string.Empty;

        // TLS settings
        cmbStreamSecurity.Text = source.StreamSecurity ?? string.Empty;
        txtSNI.Text = source.Sni ?? string.Empty;
        cmbAllowInsecure.Text = source.AllowInsecure ?? string.Empty;
        cmbFingerprint.Text = source.Fingerprint ?? string.Empty;
        cmbAlpn.Text = source.Alpn ?? string.Empty;
        txtEchConfigList.Text = source.EchConfigList ?? string.Empty;
        cmbEchForceQuery.Text = source.EchForceQuery ?? string.Empty;

        // Reality settings
        txtSNI2.Text = source.Sni ?? string.Empty;
        cmbFingerprint2.Text = source.Fingerprint ?? string.Empty;
        txtPublicKey.Text = source.PublicKey ?? string.Empty;
        txtShortId.Text = source.ShortId ?? string.Empty;
        txtSpiderX.Text = source.SpiderX ?? string.Empty;
        txtMldsa65Verify.Text = source.Mldsa65Verify ?? string.Empty;

        _isUpdatingUI = false;

        // Update cert fields
        UpdateCertFields();
    }

    private void UpdateCertFields()
    {
        if (ViewModel == null) return;
        txtCertSha256Pinning.Text = ViewModel.CertSha ?? string.Empty;
        labCertPinning.Text = ViewModel.CertTip ?? "Certificate Pinning (SHA256)";
        txtCert.Text = ViewModel.Cert ?? string.Empty;
    }

    private void SetupEventHandlers()
    {
        // GUID buttons
        btnGUID.Click += BtnGUID_Click;
        btnGUID5.Click += BtnGUID_Click;

        // Navigation buttons
        btnCancel.Click += (s, e) => CloseWithResult(false);
        
        // Bind save button to ViewModel command
        btnSave.Click += async (s, e) =>
        {
            if (ViewModel != null)
            {
                SaveDataToViewModel();
                ViewModel.SaveCmd.Execute().Subscribe(_ => { }, ex => { });
            }
        };

        // Bind fetch cert buttons to ViewModel commands
        btnFetchCert.Click += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.FetchCertCmd.Execute().Subscribe(_ => UpdateCertFields(), ex => { });
            }
        };

        btnFetchCertChain.Click += (s, e) =>
        {
            if (ViewModel != null)
            {
                ViewModel.FetchCertChainCmd.Execute().Subscribe(_ => UpdateCertFields(), ex => { });
            }
        };

        // ComboBox change events
        cmbNetwork.SelectionChanged += CmbNetwork_SelectionChanged;
        cmbStreamSecurity.SelectionChanged += CmbStreamSecurity_SelectionChanged;

        // Text change events for data binding back to ViewModel
        txtRemarks.TextChanged += (s, e) => { if (!_isUpdatingUI && ViewModel != null) ViewModel.SelectedSource.Remarks = txtRemarks.Text; };
        txtAddress.TextChanged += (s, e) => { if (!_isUpdatingUI && ViewModel != null) ViewModel.SelectedSource.Address = txtAddress.Text; };
        txtPort.TextChanged += (s, e) => { if (!_isUpdatingUI && ViewModel != null && int.TryParse(txtPort.Text, out int port)) ViewModel.SelectedSource.Port = port; };
    }

    private void SaveDataToViewModel()
    {
        if (ViewModel?.SelectedSource == null) return;

        ProfileItem source = ViewModel.SelectedSource;

        // Basic settings
        source.Remarks = txtRemarks.Text;
        source.Address = txtAddress.Text;
        if (int.TryParse(txtPort.Text, out int port))
        {
            source.Port = port;
        }

        ViewModel.CoreType = cmbCoreType.Text;

        // Protocol-specific
        switch (_profileItem.ConfigType)
        {
            case EConfigType.VMess:
                source.Id = txtId.Text;
                if (int.TryParse(txtAlterId.Text, out int alterId))
                {
                    source.AlterId = alterId;
                }
                source.Security = cmbSecurity.Text;
                source.MuxEnabled = togmuxEnabled.IsChecked == true;
                break;

            case EConfigType.Shadowsocks:
                source.Id = txtId3.Text;
                source.Security = cmbSecurity3.Text;
                source.MuxEnabled = togmuxEnabled3.IsChecked == true;
                break;

            case EConfigType.SOCKS:
            case EConfigType.HTTP:
                source.Id = txtId4.Text;
                source.Security = txtSecurity4.Text;
                break;

            case EConfigType.VLESS:
                source.Id = txtId5.Text;
                source.Flow = cmbFlow5.Text;
                source.Security = txtSecurity5.Text;
                source.MuxEnabled = togmuxEnabled5.IsChecked == true;
                break;

            case EConfigType.Trojan:
                source.Id = txtId6.Text;
                source.Flow = cmbFlow6.Text;
                source.MuxEnabled = togmuxEnabled6.IsChecked == true;
                break;

            case EConfigType.Hysteria2:
                source.Id = txtId7.Text;
                source.Path = txtPath7.Text;
                source.Ports = txtPorts7.Text;
                break;

            case EConfigType.TUIC:
                source.Id = txtId8.Text;
                source.Security = txtSecurity8.Text;
                source.HeaderType = cmbHeaderType8.Text;
                break;

            case EConfigType.WireGuard:
                source.Id = txtId9.Text;
                source.PublicKey = txtPublicKey9.Text;
                source.Path = txtPath9.Text;
                source.RequestHost = txtRequestHost9.Text;
                source.ShortId = txtShortId9.Text;
                break;

            case EConfigType.Anytls:
                source.Id = txtId10.Text;
                break;
        }

        // Transport settings
        source.Network = cmbNetwork.Text;
        source.HeaderType = cmbHeaderType.Text;
        source.RequestHost = txtRequestHost.Text;
        source.Path = txtPath.Text;
        source.Extra = txtExtra.Text;

        // TLS settings
        source.StreamSecurity = cmbStreamSecurity.Text;
        source.Sni = txtSNI.Text;
        source.AllowInsecure = cmbAllowInsecure.Text;
        source.Fingerprint = cmbFingerprint.Text;
        source.Alpn = cmbAlpn.Text;
        source.EchConfigList = txtEchConfigList.Text;
        source.EchForceQuery = cmbEchForceQuery.Text;

        // Reality settings (reuse some TLS fields)
        if (cmbStreamSecurity.Text == Global.StreamSecurityReality)
        {
            source.Sni = txtSNI2.Text;
            source.Fingerprint = cmbFingerprint2.Text;
        }
        source.PublicKey = txtPublicKey.Text;
        source.ShortId = txtShortId.Text;
        source.SpiderX = txtSpiderX.Text;
        source.Mldsa65Verify = txtMldsa65Verify.Text;

        // Cert fields
        ViewModel.CertSha = txtCertSha256Pinning.Text;
        ViewModel.Cert = txtCert.Text;
    }

    private void UpdateVisibility()
    {
        gridVMess.Visibility = Visibility.Collapsed;
        gridVLESS.Visibility = Visibility.Collapsed;
        gridSs.Visibility = Visibility.Collapsed;
        gridSocks.Visibility = Visibility.Collapsed;
        gridTrojan.Visibility = Visibility.Collapsed;
        gridHysteria2.Visibility = Visibility.Collapsed;
        gridTuic.Visibility = Visibility.Collapsed;
        gridWireguard.Visibility = Visibility.Collapsed;
        gridAnytls.Visibility = Visibility.Collapsed;
    }

    private void BtnGUID_Click(object sender, RoutedEventArgs e)
    {
        string guid = ServiceLib.Common.Utils.GetGuid();
        txtId.Text = guid;
        txtId5.Text = guid;
    }

    private void CmbNetwork_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SetHeaderType();
        SetTips();
    }

    private void CmbStreamSecurity_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string security = cmbStreamSecurity.SelectedItem?.ToString() ?? string.Empty;
        if (security == Global.StreamSecurityReality)
        {
            gridRealityMore.Visibility = Visibility.Visible;
            gridTlsMore.Visibility = Visibility.Collapsed;
        }
        else if (security == Global.StreamSecurity)
        {
            gridRealityMore.Visibility = Visibility.Collapsed;
            gridTlsMore.Visibility = Visibility.Visible;
        }
        else
        {
            gridRealityMore.Visibility = Visibility.Collapsed;
            gridTlsMore.Visibility = Visibility.Collapsed;
        }
    }

    private void SetHeaderType()
    {
        List<string> lstHeaderType = new List<string>();

        string network = cmbNetwork.SelectedItem?.ToString() ?? string.Empty;
        if (network.IsNullOrEmpty())
        {
            lstHeaderType.Add(Global.None);
            cmbHeaderType.ItemsSource = lstHeaderType;
            cmbHeaderType.SelectedIndex = 0;
            return;
        }

        if (network == nameof(ETransport.tcp))
        {
            lstHeaderType.Add(Global.None);
            lstHeaderType.Add(Global.TcpHeaderHttp);
        }
        else if (network is nameof(ETransport.kcp) or nameof(ETransport.quic))
        {
            lstHeaderType.Add(Global.None);
            lstHeaderType.AddRange(Global.KcpHeaderTypes);
        }
        else if (network is nameof(ETransport.xhttp))
        {
            lstHeaderType.AddRange(Global.XhttpMode);
        }
        else if (network == nameof(ETransport.grpc))
        {
            lstHeaderType.Add(Global.GrpcGunMode);
            lstHeaderType.Add(Global.GrpcMultiMode);
        }
        else
        {
            lstHeaderType.Add(Global.None);
        }

        cmbHeaderType.ItemsSource = lstHeaderType;
        cmbHeaderType.SelectedIndex = 0;
    }

    private void SetTips()
    {
        string network = cmbNetwork.SelectedItem?.ToString() ?? string.Empty;
        if (network.IsNullOrEmpty())
        {
            network = Global.DefaultNetwork;
        }

        labHeaderType.Visibility = Visibility.Visible;
        popExtra.Visibility = Visibility.Collapsed;

        string requestHostTip = string.Empty;
        string pathTip = string.Empty;

        switch (network)
        {
            case nameof(ETransport.tcp):
                requestHostTip = " (HTTP headers Host)";
                break;

            case nameof(ETransport.kcp):
                pathTip = " (seed)";
                break;

            case nameof(ETransport.ws):
            case nameof(ETransport.httpupgrade):
                requestHostTip = " (WebSocket/HTTP headers Host)";
                pathTip = " (WebSocket/HTTP path)";
                break;

            case nameof(ETransport.xhttp):
                requestHostTip = " (HTTP headers Host)";
                pathTip = " (HTTP path)";
                labHeaderType.Visibility = Visibility.Collapsed;
                popExtra.Visibility = Visibility.Visible;
                break;

            case nameof(ETransport.h2):
                requestHostTip = " (HTTP/2 Host)";
                pathTip = " (HTTP/2 path)";
                break;

            case nameof(ETransport.quic):
                requestHostTip = " (QUIC security)";
                pathTip = " (QUIC key)";
                break;

            case nameof(ETransport.grpc):
                requestHostTip = " (gRPC authority)";
                pathTip = " (gRPC serviceName)";
                labHeaderType.Visibility = Visibility.Collapsed;
                break;
        }

        SetRunText(tipRequestHost, requestHostTip);
        SetRunText(tipPath, pathTip);
    }

    private void SetRunText(Run run, string text)
    {
        try
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                run.Text = text;
            });
        }
        catch { }
    }

    public Task<bool> ShowDialogAsync(Window? owner, int width, int height)
    {
        _closeCompletionSource = new TaskCompletionSource<bool>();
        _dialogResult = false;

        if (owner != null)
        {
            ModalWindowHelper.ShowModal(this, owner, width, height);
        }
        else
        {
            Activate();
        }

        return _closeCompletionSource.Task;
    }

    private Task<bool> UpdateViewHandler(EViewAction action, object? obj)
    {
        switch (action)
        {
            case EViewAction.CloseWindow:
                CloseWithResult(true);
                break;
        }

        return Task.FromResult(true);
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
