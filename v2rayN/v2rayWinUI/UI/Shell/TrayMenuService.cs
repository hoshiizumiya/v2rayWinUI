using DevWinUI;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using v2rayWinUI.Views.Tray;

namespace v2rayWinUI.Services;

internal sealed class TrayMenuService : IDisposable
{
    private readonly MainWindow _owner;
    private readonly IExceptionReporter _exceptionReporter;
    private SystemTrayIcon? _trayIcon;
    private TrayMenuFlyout? _trayMenuFlyout;

    public TrayMenuService(MainWindow owner, IExceptionReporter exceptionReporter)
    {
        _owner = owner;
        ThemeService themeService = new ThemeService();
        themeService.Initialize(owner);

        _exceptionReporter = exceptionReporter;
    }

    public void Initialize()
    {
        uint trayId = 1;
        IconId iconId = WindowHelper.GetWindowIcon(_owner);
        _trayIcon = new SystemTrayIcon(trayId, iconId, _owner.Title);
        _trayIcon.IsVisible = true;

        _trayIcon.LeftClick += OnLeftClick;
        _trayIcon.RightClick += OnRightClick;
    }

    public void Dispose()
    {
        if (_trayIcon != null)
        {
            try
            {
                _trayIcon.LeftClick -= OnLeftClick;
                _trayIcon.RightClick -= OnRightClick;
                _trayIcon.Dispose();
            }
            catch (Exception ex)
            {
                _exceptionReporter.Report(ex, "TrayMenuService.Dispose");
            }
            _trayIcon = null;
        }
        _trayMenuFlyout = null;
    }

    private void OnLeftClick(SystemTrayIcon sender, SystemTrayIconEventArgs e)
    {
        Enqueue(() => _owner.Activate());
    }

    private void OnRightClick(SystemTrayIcon sender, SystemTrayIconEventArgs e)
    {
        try
        {
            _trayMenuFlyout ??= new TrayMenuFlyout();
            e.Flyout = _trayMenuFlyout;
        }
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "TrayMenuService.OnRightClick");
        }

    }

    private void Enqueue(Action action)
    {
        try
        {
            DispatcherQueue dispatcherQueue = _owner.DispatcherQueue;
            if (dispatcherQueue != null)
            {
                dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    try
                    { action(); }
                    catch (Exception ex) { _exceptionReporter.Report(ex, "TrayMenuService.Enqueue"); }
                });
                return;
            }
        }
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "TrayMenuService.Enqueue");
        }

        try
        {
            action();
        }
        catch (Exception ex)
        {
            _exceptionReporter.Report(ex, "TrayMenuService.EnqueueFallback");
        }
    }
}
