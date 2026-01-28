using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ServiceLib.Handler;
using ServiceLib.Manager;
using ServiceLib.Models;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace v2rayWinUI.ViewModels;

public sealed class GeneralSettingsPageViewModel : ReactiveObject
{
    private readonly Config _config;
    private bool _suppressSave;

    [Reactive] public bool EnableMux { get; set; }
    [Reactive] public bool EnableLogging { get; set; }

    [Reactive] public int SocksPort { get; set; }
    [Reactive] public int HttpPort { get; set; }
    [Reactive] public bool AllowLanConn { get; set; }

    public GeneralSettingsPageViewModel()
    {
        _config = AppManager.Instance.Config;

        LoadFromConfig();

        this.WhenAnyValue(x => x.EnableMux, x => x.EnableLogging, x => x.SocksPort, x => x.HttpPort, x => x.AllowLanConn)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(350), RxApp.MainThreadScheduler)
            .Subscribe(async _ => await SaveAsync());
    }

    private void LoadFromConfig()
    {
        _suppressSave = true;
        try
        {
            EnableLogging = _config.CoreBasicItem?.LogEnabled ?? false;
            EnableMux = _config.CoreBasicItem?.MuxEnabled ?? false;

            InItem? socksInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "socks");
            SocksPort = socksInbound?.LocalPort ?? 10808;
            AllowLanConn = socksInbound?.AllowLANConn ?? false;

            InItem? httpInbound = _config.Inbound?.FirstOrDefault(x => x.Protocol == "http");
            HttpPort = httpInbound?.LocalPort ?? 10809;
        }
        finally
        {
            _suppressSave = false;
        }
    }

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
