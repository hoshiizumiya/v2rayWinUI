using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using System.Threading;
using System.Threading.Tasks;

namespace v2rayWinUI.ViewModels;

public sealed partial class UpdateSettingsPageViewModel : ObservableObject
{
    private readonly Config _config;
    private bool _suppressSave;


    private int _autoUpdateInterval;
    private string _geoSourceUrl = string.Empty;
    private string _srsSourceUrl = string.Empty;

    private bool _updateV2rayN;
    private bool _updateXray;
    private bool _updateMihomo;
    private bool _updateSingbox;
    private bool _updateGeoFiles;

    private bool _includePreRelease;
    private bool _isBusy;

    private bool _isProgressVisible;
    private string _statusText = string.Empty;

    private string _geoLastUpdateText = "-";

    public int AutoUpdateInterval
    {
        get => _autoUpdateInterval;
        set
        {
            if (SetProperty(ref _autoUpdateInterval, value))
            {
                _ = SaveAllAsync();
            }
        }
    }

    public string GeoSourceUrl
    {
        get => _geoSourceUrl;
        set
        {
            if (SetProperty(ref _geoSourceUrl, value))
            {
                _ = SaveAllAsync();
            }
        }
    }

    public string SrsSourceUrl
    {
        get => _srsSourceUrl;
        set
        {
            if (SetProperty(ref _srsSourceUrl, value))
            {
                _ = SaveAllAsync();
            }
        }
    }

    public bool UpdateV2rayN
    {
        get => _updateV2rayN;
        set
        {
            if (SetProperty(ref _updateV2rayN, value))
            {
                _ = SaveAllAsync();
            }
        }
    }

    public bool UpdateXray
    {
        get => _updateXray;
        set
        {
            if (SetProperty(ref _updateXray, value))
            {
                _ = SaveAllAsync();
            }
        }
    }

    public bool UpdateMihomo
    {
        get => _updateMihomo;
        set
        {
            if (SetProperty(ref _updateMihomo, value))
            {
                _ = SaveAllAsync();
            }
        }
    }

    public bool UpdateSingbox
    {
        get => _updateSingbox;
        set
        {
            if (SetProperty(ref _updateSingbox, value))
            {
                _ = SaveAllAsync();
            }
        }
    }

    public bool UpdateGeoFiles
    {
        get => _updateGeoFiles;
        set
        {
            if (SetProperty(ref _updateGeoFiles, value))
            {
                _ = SaveAllAsync();
            }
        }
    }

    public bool IncludePreRelease
    {
        get => _includePreRelease;
        set
        {
            if (SetProperty(ref _includePreRelease, value))
            {
                _ = SaveAllAsync();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsProgressVisible
    {
        get => _isProgressVisible;
        private set => SetProperty(ref _isProgressVisible, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string GeoLastUpdateText
    {
        get => _geoLastUpdateText;
        private set => SetProperty(ref _geoLastUpdateText, value);
    }

    public UpdateSettingsPageViewModel()
    {
        _config = AppManager.Instance.Config;

        LoadFromConfig();
    }

    // No debounce: save immediately on each setting change.

    private async Task SaveAllAsync()
    {
        if (_suppressSave)
        {
            return;
        }

        await SaveCoreSettingsAsync();
        await SaveSelectionAsync();
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

    private async Task CheckEnabledUpdatesAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        IsProgressVisible = true;
        StatusText = "Checking updates...";
        try
        {
            UpdateService updateService = new UpdateService(_config, async (_, __) => await Task.CompletedTask);

            if (UpdateV2rayN)
            {
                StatusText = "Checking v2rayN...";
                await updateService.CheckUpdateGuiN(IncludePreRelease);
            }

            if (UpdateXray)
            {
                StatusText = "Checking Xray...";
                await updateService.CheckUpdateCore(ECoreType.Xray, IncludePreRelease);
            }

            if (UpdateMihomo)
            {
                StatusText = "Checking mihomo...";
                await updateService.CheckUpdateCore(ECoreType.mihomo, false);
            }

            if (UpdateSingbox)
            {
                StatusText = "Checking sing-box...";
                await updateService.CheckUpdateCore(ECoreType.sing_box, false);
            }

            if (UpdateGeoFiles)
            {
                StatusText = "Updating GeoFiles...";
                await updateService.UpdateGeoFileAll();
                RefreshGeoStatus();
            }

            StatusText = "Done";
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
        finally
        {
            IsProgressVisible = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        await CheckEnabledUpdatesAsync();
    }
}
