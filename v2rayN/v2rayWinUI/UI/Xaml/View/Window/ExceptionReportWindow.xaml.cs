// Copyright (c) v2rayWinUI Contributors. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Sentry;
using v2rayWinUI.Core.ExceptionService;
using v2rayWinUI.ViewModels;
using ServiceLib.Common;
using Windows.Graphics;

namespace v2rayWinUI.UI.Xaml.View.Window;

/// <summary>
/// Exception report window - displays unhandled exceptions and allows user feedback
/// Inspired by SnapHutao's ExceptionWindow
/// </summary>
public sealed partial class ExceptionReportWindow : Microsoft.UI.Xaml.Window
{
    public ExceptionReportViewModel ViewModel { get; }

    public ExceptionReportWindow(SentryId eventId, Exception exception)
    {
        InitializeComponent();

        ViewModel = new ExceptionReportViewModel(eventId, exception);

        AppWindow.Title = "v2rayWinUI - Exception Report";

        AppWindowTitleBar titleBar = AppWindow.TitleBar;
        titleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        titleBar.ExtendsContentIntoTitleBar = true;

        // Kill process when window closes (matching SnapHutao pattern)
        Closed += (_, _) =>
        {
            Environment.Exit(1);
        };

        SizeInt32 size = new(800, 500);
        AppWindow.Resize(size);

        // Wire up IsEnabled binding for SendReportButton
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExceptionReportViewModel.IsSendingReport))
            {
                SendReportButton.IsEnabled = !ViewModel.IsSendingReport;
            }
        };

        // Initial state
        SendReportButton.IsEnabled = !ViewModel.IsSendingReport;
    }

    public static void Show(CapturedException capturedException)
    {
        Show(capturedException.Id, capturedException.Exception);
    }

    public static void Show(SentryId id, Exception exception)
    {
        try
        {
            ExceptionReportWindow window = new(id, exception);
            // Use Show(false) for non-modal window - no owner required
            window.AppWindow.Show(false);
            window.AppWindow.MoveInZOrderAtTop();
        }
        catch (Exception ex)
        {
            // If window creation fails, just log and exit
            Logging.SaveLog($"Failed to show exception window: {ex}");
            Environment.Exit(1);
        }
    }

    private async void OnSendReportClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.SendReportCommand.ExecuteAsync(null);
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

