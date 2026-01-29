using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ServiceLib.Common;
using ServiceLib.Events;
using ServiceLib.Models;
using System;

namespace v2rayWinUI.Views;

 public sealed partial class DashboardView : Page
{
    public event Action<string>? NavigateRequested;

    public DashboardView()
    {
        InitializeComponent();

        btnGoServers.Click += (_, _) => NavigateRequested?.Invoke("servers");
        btnGoSubs.Click += (_, _) => NavigateRequested?.Invoke("subs");
        btnGoLog.Click += (_, _) => NavigateRequested?.Invoke("log");

        try
        {
            AppEvents.DispatcherStatisticsRequested
                .AsObservable()
                .Subscribe(update =>
                {
                    DispatcherQueue.TryEnqueue(() => UpdateSpeed(update));
                });
        }
        catch { }
    }

    private void UpdateSpeed(ServerSpeedItem? speedItem)
    {
        if (speedItem == null)
        {
            return;
        }

        try
        {
            string upSpeed = Utils.HumanFy(speedItem.ProxyUp);
            string downSpeed = Utils.HumanFy(speedItem.ProxyDown);

            if (speedGraph != null)
            {
                speedGraph.SpeedText = $"↑ {upSpeed}/s  ↓ {downSpeed}/s";
                ulong speed = (ulong)(speedItem.ProxyUp + speedItem.ProxyDown);
                speedGraph.AddPoint(speed, speed);
            }
        }
        catch { }
    }
}
