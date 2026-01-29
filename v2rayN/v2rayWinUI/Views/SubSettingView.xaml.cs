using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Models;
using ServiceLib.ViewModels;

namespace v2rayWinUI.Views;

public sealed partial class SubSettingView : Page
{
    private SubSettingViewModel? _viewModel;

    public SubSettingView()
    {
        InitializeComponent();
        Loaded += SubSettingView_Loaded;
    }

    private void SubSettingView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null)
        {
            // If not bound by MainView, create a local one as fallback.
            _viewModel = new SubSettingViewModel(UpdateViewHandler);
            BindData(_viewModel);
        }

        SubSettingViewAddButton.Click -= AddButton_Click;
        SubSettingViewAddButton.Click += AddButton_Click;
        SubSettingViewEditButton.Click -= EditButton_Click;
        SubSettingViewEditButton.Click += EditButton_Click;
        SubSettingViewDeleteButton.Click -= DeleteButton_Click;
        SubSettingViewDeleteButton.Click += DeleteButton_Click;

        SubSettingViewList.SelectionChanged -= List_SelectionChanged;
        SubSettingViewList.SelectionChanged += List_SelectionChanged;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) => _viewModel?.SubAddCmd.Execute().Subscribe();
    private void EditButton_Click(object sender, RoutedEventArgs e) => _viewModel?.SubEditCmd.Execute().Subscribe();
    private void DeleteButton_Click(object sender, RoutedEventArgs e) => _viewModel?.SubDeleteCmd.Execute().Subscribe();

    private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null) return;
        _viewModel.SelectedSources = SubSettingViewList.SelectedItems.Cast<SubItem>().ToList();
        _viewModel.SelectedSource = (SubItem?)SubSettingViewList.SelectedItem ?? new SubItem();
    }

    public void BindData(SubSettingViewModel? viewModel)
    {
        if (viewModel == null) return;
        _viewModel = viewModel;
        SubSettingViewList.ItemsSource = _viewModel.SubItems;

        if (_viewModel.SubItems.Count == 0)
        {
            _ = _viewModel.RefreshSubItems();
        }

        try
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        catch { }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(SubSettingViewModel.SubItems))
            {
                SubSettingViewList.ItemsSource = _viewModel?.SubItems;
            }
        }
        catch { }
    }

    private Task<bool> UpdateViewHandler(ServiceLib.Enums.EViewAction action, object? obj)
    {
        return ((App.Current as v2rayWinUI.App)?.MainWindowHandler?.Invoke(action, obj)) ?? Task.FromResult(false);
    }
}
