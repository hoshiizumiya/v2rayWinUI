using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using ServiceLib.Models;
using ServiceLib.ViewModels;
using System.Threading.Tasks;
using v2rayWinUI.ViewModels;

namespace v2rayWinUI.Views;

public sealed partial class SubSettingView : Page
{
    private SubSettingViewModel? _serviceVm;
    private SubSettingPageViewModel? _pageVm;
    public SubSettingViewModel? ServiceViewModel { get; private set; }

    public SubSettingView()
    {
        InitializeComponent();
        Loaded += SubSettingView_Loaded;
    }

    private void SubSettingView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_serviceVm == null)
        {
            // If not bound by MainView, create a local one as fallback.
            _serviceVm = new SubSettingViewModel(UpdateViewHandler);
            BindData(_serviceVm);
        }

        SubSettingViewList.SelectionChanged -= List_SelectionChanged;
        SubSettingViewList.SelectionChanged += List_SelectionChanged;
    }

    private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_serviceVm == null) return;
        _serviceVm.SelectedSources = SubSettingViewList.SelectedItems.Cast<SubItem>().ToList();
        _serviceVm.SelectedSource = (SubItem?)SubSettingViewList.SelectedItem ?? new SubItem();
        _pageVm?.UpdateCanEditRemove();
    }

    public void BindData(SubSettingViewModel? viewModel)
    {
        if (viewModel == null) return;
        _serviceVm = viewModel;
        ServiceViewModel = viewModel;
        _pageVm = new SubSettingPageViewModel(_serviceVm, ShowErrorAsync);
        DataContext = _pageVm;

        SubSettingViewList.ItemsSource = _serviceVm.SubItems;

        if (_serviceVm.SubItems.Count == 0)
        {
            _ = _serviceVm.RefreshSubItems();
        }

        try
        {
            _serviceVm.PropertyChanged -= ViewModel_PropertyChanged;
            _serviceVm.PropertyChanged += ViewModel_PropertyChanged;
        }
        catch { }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(SubSettingViewModel.SubItems))
            {
                SubSettingViewList.ItemsSource = _serviceVm?.SubItems;
            }
        }
        catch { }
    }

    private Task<bool> UpdateViewHandler(ServiceLib.Enums.EViewAction action, object? obj)
    {
        return ((App.Current as v2rayWinUI.App)?.MainWindowHandler?.Invoke(action, obj)) ?? Task.FromResult(false);
    }

    private async Task ShowErrorAsync(Exception? ex)
    {
        try
        {
            string title = "Error";
            string unknown = "Unknown error";
            string ok = "OK";
            try
            {
                var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                title = loader.GetString("v2rayWinUI.Common.Error");
                unknown = loader.GetString("v2rayWinUI.Common.UnknownError");
                ok = loader.GetString("v2rayWinUI.Common.OK");
            }
            catch { }

            var dlg = new ContentDialog
            {
                Title = title,
                Content = ex?.Message ?? unknown,
                CloseButtonText = ok,
                XamlRoot = this.XamlRoot
            };
            await dlg.ShowAsync();
        }
        catch { }
    }
}
