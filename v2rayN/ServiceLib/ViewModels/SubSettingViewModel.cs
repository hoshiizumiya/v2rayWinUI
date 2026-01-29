namespace ServiceLib.ViewModels;

public class SubSettingViewModel : MyReactiveObject
{
    [Reactive]
    public System.Collections.ObjectModel.ObservableCollection<SubItem> SubItems { get; private set; } = new System.Collections.ObjectModel.ObservableCollection<SubItem>();

    [Reactive]
    public SubItem SelectedSource { get; set; }

    public IList<SubItem> SelectedSources { get; set; }

    public ReactiveCommand<Unit, Unit> SubAddCmd { get; }
    public ReactiveCommand<Unit, Unit> SubDeleteCmd { get; }
    public ReactiveCommand<Unit, Unit> SubEditCmd { get; }
    public ReactiveCommand<Unit, Unit> SubShareCmd { get; }
    public bool IsModified { get; set; }

    public SubSettingViewModel(Func<EViewAction, object?, Task<bool>>? updateView)
    {
        _config = AppManager.Instance.Config;
        _updateView = updateView;

        var canEditRemove = this.WhenAnyValue(
           x => x.SelectedSource,
           selectedSource => selectedSource != null && !selectedSource.Id.IsNullOrEmpty());

        SubAddCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await EditSubAsync(true);
        });
        SubDeleteCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await DeleteSubAsync();
        }, canEditRemove);
        SubEditCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await EditSubAsync(false);
        }, canEditRemove);
        SubShareCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await _updateView?.Invoke(EViewAction.ShareSub, SelectedSource?.Url);
        }, canEditRemove);

        try
        {
            AppEvents.SubscriptionsRefreshRequested
                .AsObservable()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ => await RefreshSubItems());
        }
        catch { }

        _ = Init();
    }

    private async Task Init()
    {
        SelectedSource = new();

        await RefreshSubItems();
    }

    public async Task RefreshSubItems()
    {
        System.Collections.Generic.List<SubItem> items = await AppManager.Instance.SubItems() ?? new System.Collections.Generic.List<SubItem>();
        SubItems = new System.Collections.ObjectModel.ObservableCollection<SubItem>(items);
    }

    public async Task EditSubAsync(bool blNew)
    {
        SubItem item;
        if (blNew)
        {
            item = new();
        }
        else
        {
            item = await AppManager.Instance.GetSubItem(SelectedSource?.Id);
            if (item is null)
            {
                return;
            }
        }
        if (await _updateView?.Invoke(EViewAction.SubEditWindow, item) == true)
        {
            await RefreshSubItems();
            IsModified = true;
            try { AppEvents.SubscriptionsRefreshRequested.Publish(); } catch { }
        }
    }

    private async Task DeleteSubAsync()
    {
        if (await _updateView?.Invoke(EViewAction.ShowYesNo, null) == false)
        {
            return;
        }

        foreach (var it in SelectedSources ?? [SelectedSource])
        {
            await ConfigHandler.DeleteSubItem(_config, it.Id);
        }
        await RefreshSubItems();
        NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
        IsModified = true;
        try { AppEvents.SubscriptionsRefreshRequested.Publish(); } catch { }
    }
}
