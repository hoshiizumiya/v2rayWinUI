using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ServiceLib.Events;
using ServiceLib.Manager;
using ServiceLib.Models;
using ServiceLib.ViewModels;

namespace v2rayWinUI.ViewModels;

public sealed class ProfilesPageViewModel : ObservableObject
{
    public ProfilesViewModel ServiceVm { get; }

    private IReadOnlyList<SubItem> _subGroups = Array.Empty<SubItem>();
    public IReadOnlyList<SubItem> SubGroups
    {
        get => _subGroups;
        private set => SetProperty(ref _subGroups, value);
    }

    private string _serverFilterText = string.Empty;
    public string ServerFilterText
    {
        get => _serverFilterText;
        set
        {
            if (SetProperty(ref _serverFilterText, value))
            {
                ServiceVm.ServerFilter = value;
            }
        }
    }

    private string _selectedSubId = string.Empty;
    public string SelectedSubId
    {
        get => _selectedSubId;
        set
        {
            if (SetProperty(ref _selectedSubId, value))
            {
                AppManager.Instance.Config.SubIndexId = value;
                AppEvents.ProfilesRefreshRequested.Publish();
            }
        }
    }

    public ProfilesPageViewModel(ProfilesViewModel serviceVm)
    {
        ServiceVm = serviceVm;
        _ = LoadSubGroupsAsync();
    }

    public async Task LoadSubGroupsAsync()
    {
        // Use ServiceVm.SubItems (already includes 'All') to keep UI consistent.
        SubGroups = ServiceVm.SubItems;
        SelectedSubId = AppManager.Instance.Config.SubIndexId ?? string.Empty;
    }

    public IRelayCommand ApplyFilterCommand => new RelayCommand(() =>
    {
        ServiceVm.SortServerResultCmd.Execute().Subscribe();
    });

    public IRelayCommand SelectAllGroupsCommand => new RelayCommand(() =>
    {
        SelectedSubId = string.Empty;
    });
}
