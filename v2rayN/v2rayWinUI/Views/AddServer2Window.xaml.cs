using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using Windows.Storage.Pickers;
using WinRT.Interop;
using ServiceLib.Common;
using ServiceLib.Enums;
using ServiceLib.Models;
using ServiceLib.ViewModels;
using v2rayWinUI.Base;

namespace v2rayWinUI.Views;

public sealed partial class AddServer2Window : ModernDialogWindow, Services.IDialogWindow
{
    public AddServer2ViewModel? ViewModel { get; private set; }

    public AddServer2Window(ProfileItem profileItem)
    {
        InitializeComponent();

        // Set up title bar
        SetTitleBar(TitleBarArea);

        ViewModel = new AddServer2ViewModel(profileItem, UpdateViewHandler);

        cmbCoreType.ItemsSource = ServiceLib.Common.Utils.GetEnumNames<ECoreType>()
            .Where(t => t != ECoreType.v2rayN.ToString())
            .ToList()
            .AppendEmpty();

        LoadFromViewModel();
        SetupHandlers();
    }

    private void SetupHandlers()
    {
        btnCancel.Click += (_, _) => CloseWithResult(false);
        btnSave.Click += (_, _) => ExecuteSave();
        btnBrowse.Click += (_, _) => ExecuteBrowse();
        btnEdit.Click += (_, _) => ExecuteEdit();

        // basic two-way sync
        txtRemarks.TextChanged += (_, _) => { if (ViewModel?.SelectedSource != null) ViewModel.SelectedSource.Remarks = txtRemarks.Text; };
        cmbCoreType.SelectionChanged += (_, _) => { if (ViewModel != null) ViewModel.CoreType = cmbCoreType.SelectedItem?.ToString() ?? string.Empty; };
        togDisplayLog.Toggled += (_, _) => { if (ViewModel?.SelectedSource != null) ViewModel.SelectedSource.DisplayLog = togDisplayLog.IsOn; };
        txtPreSocksPort.TextChanged += (_, _) =>
        {
            if (ViewModel?.SelectedSource == null)
            {
                return;
            }

            if (int.TryParse(txtPreSocksPort.Text, out int port))
            {
                ViewModel.SelectedSource.PreSocksPort = port;
            }
            else
            {
                ViewModel.SelectedSource.PreSocksPort = null;
            }
        };
        txtConfig.TextChanged += (_, _) => { if (ViewModel?.SelectedSource != null) ViewModel.SelectedSource.Address = txtConfig.Text; };
    }

    private void LoadFromViewModel()
    {
        if (ViewModel?.SelectedSource == null)
        {
            return;
        }

        ProfileItem src = ViewModel.SelectedSource;
        txtRemarks.Text = src.Remarks ?? string.Empty;
        cmbCoreType.Text = ViewModel.CoreType ?? string.Empty;
        togDisplayLog.IsOn = src.DisplayLog;
        txtPreSocksPort.Text = src.PreSocksPort?.ToString() ?? string.Empty;
        txtConfig.Text = src.Address ?? string.Empty;
    }

    private void ExecuteSave()
    {
        try
        {
            ViewModel?.SaveServerCmd.Execute().Subscribe(
                _ => { },
                ex => { }
            );
        }
        catch
        {
            // swallow - ReactiveUI global handler will otherwise crash the app
        }
    }

    private void ExecuteBrowse()
    {
        _ = UpdateViewHandler(EViewAction.BrowseServer, null);
    }

    private void ExecuteEdit()
    {
        try
        {
            ViewModel?.EditServerCmd.Execute().Subscribe(_ => { }, ex => { });
        }
        catch
        {
        }
    }

    private Task<bool> UpdateViewHandler(EViewAction action, object? obj)
    {
        switch (action)
        {
            case EViewAction.CloseWindow:
                CloseWithResult(true);
                break;

            case EViewAction.BrowseServer:
                _ = BrowseServerAsync();
                break;
        }

        return Task.FromResult(true);
    }

    private async Task BrowseServerAsync()
    {
        try
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".json");
            picker.FileTypeFilter.Add(".yaml");
            picker.FileTypeFilter.Add(".yml");
            picker.FileTypeFilter.Add(".*");

            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hWnd);

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                return;
            }

            if (ViewModel != null)
            {
                await ViewModel.BrowseServer(file.Path);
                LoadFromViewModel();
            }
        }
        catch
        {
            // ignore
        }
    }
}
