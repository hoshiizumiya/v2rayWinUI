using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using ServiceLib.Handler;
using ServiceLib.Manager;
using ServiceLib.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace v2rayWinUI.ViewModels;

public sealed partial class GeneralSettingsPageViewModel : ObservableObject
{
    private readonly Config _config;
    private bool _suppressSave;

    public GeneralSettingsPageViewModel()
    {
        _config = AppManager.Instance.Config;

        LoadFromConfig();
    }

    [ObservableProperty] private bool enableMux;
    [ObservableProperty] private bool enableLogging;

    [ObservableProperty] private string logLevel = string.Empty;
    [ObservableProperty] private bool defaultAllowInsecure;
    [ObservableProperty] private string defaultFingerprint = string.Empty;
    [ObservableProperty] private string defaultUserAgent = string.Empty;

    [ObservableProperty] private string mux4SboxProtocol = string.Empty;
    [ObservableProperty] private bool enableCacheFile4Sbox;

    [ObservableProperty] private int hyUpMbps;
    [ObservableProperty] private int hyDownMbps;
    [ObservableProperty] private bool enableFragment;

    [ObservableProperty] private bool udpEnabled;
    [ObservableProperty] private bool sniffingEnabled;
    [ObservableProperty] private bool routeOnly;
    [ObservableProperty] private bool newPort4Lan;
    [ObservableProperty] private string inboundUser = string.Empty;
    [ObservableProperty] private string inboundPass = string.Empty;

    [ObservableProperty] private int socksPort;
    [ObservableProperty] private int httpPort;
    [ObservableProperty] private bool allowLanConn;

    [ObservableProperty] private bool enableStatistics;
    [ObservableProperty] private bool displayRealTimeSpeed;

    partial void OnEnableMuxChanged(bool value) => _ = SaveAsync();
    partial void OnEnableLoggingChanged(bool value) => _ = SaveAsync();
    partial void OnLogLevelChanged(string value) => _ = SaveAsync();
    partial void OnDefaultAllowInsecureChanged(bool value) => _ = SaveAsync();
    partial void OnDefaultFingerprintChanged(string value) => _ = SaveAsync();
    partial void OnDefaultUserAgentChanged(string value) => _ = SaveAsync();
    partial void OnMux4SboxProtocolChanged(string value) => _ = SaveAsync();
    partial void OnEnableCacheFile4SboxChanged(bool value) => _ = SaveAsync();
    partial void OnHyUpMbpsChanged(int value) => _ = SaveAsync();
    partial void OnHyDownMbpsChanged(int value) => _ = SaveAsync();
    partial void OnEnableFragmentChanged(bool value) => _ = SaveAsync();
    partial void OnUdpEnabledChanged(bool value) => _ = SaveAsync();
    partial void OnSniffingEnabledChanged(bool value) => _ = SaveAsync();
    partial void OnRouteOnlyChanged(bool value) => _ = SaveAsync();
    partial void OnNewPort4LanChanged(bool value) => _ = SaveAsync();
    partial void OnInboundUserChanged(string value) => _ = SaveAsync();
    partial void OnInboundPassChanged(string value) => _ = SaveAsync();
    partial void OnSocksPortChanged(int value) => _ = SaveAsync();
    partial void OnHttpPortChanged(int value) => _ = SaveAsync();
    partial void OnAllowLanConnChanged(bool value) => _ = SaveAsync();
    partial void OnEnableStatisticsChanged(bool value) => _ = SaveAsync();
    partial void OnDisplayRealTimeSpeedChanged(bool value) => _ = SaveAsync();

    private void LoadFromConfig()
    {
        _suppressSave = true;
        try
        {
            EnableLogging = _config.CoreBasicItem?.LogEnabled ?? false;
            EnableMux = _config.CoreBasicItem?.MuxEnabled ?? false;

            LogLevel = _config.CoreBasicItem?.Loglevel ?? string.Empty;
            DefaultAllowInsecure = _config.CoreBasicItem?.DefAllowInsecure ?? false;
            DefaultFingerprint = _config.CoreBasicItem?.DefFingerprint ?? string.Empty;
            DefaultUserAgent = _config.CoreBasicItem?.DefUserAgent ?? string.Empty;

            Mux4SboxProtocol = _config.Mux4SboxItem?.Protocol ?? string.Empty;
            EnableCacheFile4Sbox = _config.CoreBasicItem?.EnableCacheFile4Sbox ?? false;

            HyUpMbps = _config.HysteriaItem?.UpMbps ?? 0;
            HyDownMbps = _config.HysteriaItem?.DownMbps ?? 0;
            EnableFragment = _config.CoreBasicItem?.EnableFragment ?? false;

            EnableStatistics = _config.GuiItem.EnableStatistics;
            DisplayRealTimeSpeed = _config.GuiItem.DisplayRealTimeSpeed;

            InItem? socksInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "socks");
            SocksPort = socksInbound?.LocalPort ?? 10808;
            AllowLanConn = socksInbound?.AllowLANConn ?? false;

            InItem? inbound0 = _config.Inbound?.FirstOrDefault();
            if (inbound0 != null)
            {
                UdpEnabled = inbound0.UdpEnabled;
                SniffingEnabled = inbound0.SniffingEnabled;
                RouteOnly = inbound0.RouteOnly;
                NewPort4Lan = inbound0.NewPort4LAN;
                InboundUser = inbound0.User ?? string.Empty;
                InboundPass = inbound0.Pass ?? string.Empty;
            }

            InItem? httpInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "http");
            HttpPort = httpInbound?.LocalPort ?? 10809;
        }
        finally
        {
            _suppressSave = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_suppressSave)
        {
            return;
        }

        if (_config.CoreBasicItem != null)
        {
            _config.CoreBasicItem.LogEnabled = EnableLogging;
            _config.CoreBasicItem.MuxEnabled = EnableMux;

            _config.CoreBasicItem.Loglevel = string.IsNullOrWhiteSpace(LogLevel) ? string.Empty : LogLevel.Trim();
            _config.CoreBasicItem.DefAllowInsecure = DefaultAllowInsecure;
            _config.CoreBasicItem.DefFingerprint = string.IsNullOrWhiteSpace(DefaultFingerprint) ? string.Empty : DefaultFingerprint.Trim();
            _config.CoreBasicItem.DefUserAgent = string.IsNullOrWhiteSpace(DefaultUserAgent) ? string.Empty : DefaultUserAgent.Trim();
            _config.CoreBasicItem.EnableCacheFile4Sbox = EnableCacheFile4Sbox;
            _config.CoreBasicItem.EnableFragment = EnableFragment;
        }

        if (_config.Mux4SboxItem != null)
        {
            _config.Mux4SboxItem.Protocol = string.IsNullOrWhiteSpace(Mux4SboxProtocol) ? string.Empty : Mux4SboxProtocol.Trim();
        }

        if (_config.HysteriaItem != null)
        {
            _config.HysteriaItem.UpMbps = HyUpMbps;
            _config.HysteriaItem.DownMbps = HyDownMbps;
        }

        _config.GuiItem.EnableStatistics = EnableStatistics;
        _config.GuiItem.DisplayRealTimeSpeed = DisplayRealTimeSpeed;

        InItem? inbound0 = _config.Inbound?.FirstOrDefault();
        if (inbound0 != null)
        {
            inbound0.UdpEnabled = UdpEnabled;
            inbound0.SniffingEnabled = SniffingEnabled;
            inbound0.RouteOnly = RouteOnly;
            inbound0.NewPort4LAN = NewPort4Lan;
            inbound0.User = string.IsNullOrWhiteSpace(InboundUser) ? null : InboundUser.Trim();
            inbound0.Pass = string.IsNullOrWhiteSpace(InboundPass) ? null : InboundPass.Trim();
        }

        InItem? socksInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "socks");
        if (socksInbound != null)
        {
            socksInbound.LocalPort = SocksPort;
            socksInbound.AllowLANConn = AllowLanConn;
        }

        InItem? httpInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "http");
        if (httpInbound != null)
        {
            httpInbound.LocalPort = HttpPort;
        }

        _ = await ConfigHandler.SaveConfig(_config);
    }
}
