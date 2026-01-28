using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Handler;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace v2rayWinUI.ViewModels;

public sealed class UpdateSettingsPageViewModel : ReactiveObject
{
    private readonly Config _config;
    private bool _suppressSave;

    [Reactive] public int AutoUpdateInterval { get; set; }
    [Reactive] public string GeoSourceUrl { get; set; } = string.Empty;
    [Reactive] public string SrsSourceUrl { get; set; } = string.Empty;

    [Reactive] public bool UpdateV2rayN { get; set; }
    [Reactive] public bool UpdateXray { get; set; }
    [Reactive] public bool UpdateMihomo { get; set; }
    [Reactive] public bool UpdateSingbox { get; set; }
    [Reactive] public bool UpdateGeoFiles { get; set; }

    [Reactive] public bool IncludePreRelease { get; set; }
    [Reactive] public bool IsBusy { get; private set; }

    [Reactive] public string GeoLastUpdateText { get; private set; } = "-";

    public ReactiveCommand<Unit, Unit> CheckV2rayNCmd { get; }
    public ReactiveCommand<Unit, Unit> CheckXrayCmd { get; }
    public ReactiveCommand<Unit, Unit> CheckMihomoCmd { get; }
    public ReactiveCommand<Unit, Unit> CheckSingboxCmd { get; }
    public ReactiveCommand<Unit, Unit> CheckGeoCmd { get; }

    public UpdateSettingsPageViewModel()
    {
        _config = AppManager.Instance.Config;

        LoadFromConfig();

        this.WhenAnyValue(x => x.AutoUpdateInterval, x => x.GeoSourceUrl, x => x.SrsSourceUrl)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(350), RxApp.MainThreadScheduler)
            .Subscribe(async _ => await SaveCoreSettingsAsync());

        this.WhenAnyValue(
                x => x.UpdateV2rayN,
                x => x.UpdateXray,
                x => x.UpdateMihomo,
                x => x.UpdateSingbox,
                x => x.UpdateGeoFiles,
                x => x.IncludePreRelease)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(350), RxApp.MainThreadScheduler)
            .Subscribe(async _ => await SaveSelectionAsync());

        CheckV2rayNCmd = ReactiveCommand.CreateFromTask(async () => await CheckUpdateAsync(ECoreType.v2rayN.ToString(), false));
        CheckXrayCmd = ReactiveCommand.CreateFromTask(async () => await CheckUpdateAsync(ECoreType.Xray.ToString(), false));
        CheckMihomoCmd = ReactiveCommand.CreateFromTask(async () => await CheckUpdateAsync(ECoreType.mihomo.ToString(), false));
        CheckSingboxCmd = ReactiveCommand.CreateFromTask(async () => await CheckUpdateAsync(ECoreType.sing_box.ToString(), false));
        CheckGeoCmd = ReactiveCommand.CreateFromTask(async () => await CheckUpdateAsync("GeoFiles", true));
    }

    private void LoadFromConfig()
    {
        _suppressSave = true;
        try
        {
            AutoUpdateInterval = _config.GuiItem.AutoUpdateInterval;
            GeoSourceUrl = _config.ConstItem.GeoSourceUrl ?? string.Empty;
            SrsSourceUrl = _config.ConstItem.SrsSourceUrl ?? string.Empty;

            HashSet<string> selected = new HashSet<string>(_config.CheckUpdateItem.SelectedCoreTypes ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            UpdateV2rayN = selected.Contains(ECoreType.v2rayN.ToString());
            UpdateXray = selected.Contains(ECoreType.Xray.ToString());
            UpdateMihomo = selected.Contains(ECoreType.mihomo.ToString());
            UpdateSingbox = selected.Contains(ECoreType.sing_box.ToString());
            UpdateGeoFiles = selected.Contains("GeoFiles");

            IncludePreRelease = _config.CheckUpdateItem.CheckPreReleaseUpdate;
            RefreshGeoStatus();
        }
        finally
        {
            _suppressSave = false;
        }
    }

    private async Task SaveCoreSettingsAsync()
    {
        if (_suppressSave)
        {
            return;
        }

        _config.GuiItem.AutoUpdateInterval = AutoUpdateInterval;
        _config.ConstItem.GeoSourceUrl = string.IsNullOrWhiteSpace(GeoSourceUrl) ? null : GeoSourceUrl.Trim();
        _config.ConstItem.SrsSourceUrl = string.IsNullOrWhiteSpace(SrsSourceUrl) ? null : SrsSourceUrl.Trim();

        _ = await ConfigHandler.SaveConfig(_config);
    }

    private async Task SaveSelectionAsync()
    {
        if (_suppressSave)
        {
            return;
        }

        _config.CheckUpdateItem.CheckPreReleaseUpdate = IncludePreRelease;

        List<string> selected = new List<string>();
        if (UpdateV2rayN) selected.Add(ECoreType.v2rayN.ToString());
        if (UpdateXray) selected.Add(ECoreType.Xray.ToString());
        if (UpdateMihomo) selected.Add(ECoreType.mihomo.ToString());
        if (UpdateSingbox) selected.Add(ECoreType.sing_box.ToString());
        if (UpdateGeoFiles) selected.Add("GeoFiles");

        _config.CheckUpdateItem.SelectedCoreTypes = selected;
        _ = await ConfigHandler.SaveConfig(_config);
    }

    public void RefreshGeoStatus()
    {
        try
        {
            string geoSitePath = Utils.GetBinPath("geosite.dat");
            string geoIpPath = Utils.GetBinPath("geoip.dat");

            DateTime? geoSiteTime = File.Exists(geoSitePath) ? File.GetLastWriteTime(geoSitePath) : null;
            DateTime? geoIpTime = File.Exists(geoIpPath) ? File.GetLastWriteTime(geoIpPath) : null;

            if (geoSiteTime == null && geoIpTime == null)
            {
                GeoLastUpdateText = "Missing";
                return;
            }

            DateTime latest = geoSiteTime ?? geoIpTime ?? DateTime.MinValue;
            if (geoIpTime != null && geoIpTime.Value > latest)
            {
                latest = geoIpTime.Value;
            }

            GeoLastUpdateText = latest.ToString("yyyy/MM/dd HH:mm:ss");
        }
        catch
        {
            GeoLastUpdateText = "-";
        }
    }

    private async Task CheckUpdateAsync(string coreType, bool isGeo)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            UpdateService updateService = new UpdateService(_config, async (_, __) => await Task.CompletedTask);

            if (isGeo)
            {
                await updateService.UpdateGeoFileAll();
                RefreshGeoStatus();
                return;
            }

            if (coreType == ECoreType.v2rayN.ToString())
            {
                await updateService.CheckUpdateGuiN(IncludePreRelease);
                return;
            }

            ECoreType parsed;
            if (Enum.TryParse<ECoreType>(coreType, out parsed))
            {
                bool pre = coreType == ECoreType.Xray.ToString() ? IncludePreRelease : false;
                await updateService.CheckUpdateCore(parsed, pre);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
