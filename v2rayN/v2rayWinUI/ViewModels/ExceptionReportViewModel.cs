// Copyright (c) v2rayWinUI Contributors. All rights reserved.
// Licensed under the MIT license.
// Inspired by SnapHutao's ExceptionWindow

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentry;
using ServiceLib.Common;
using System;
using System.Threading.Tasks;

namespace v2rayWinUI.ViewModels;

internal sealed partial class ExceptionReportViewModel : ObservableObject
{
    private readonly SentryId associatedEventId;

    // public event Action? ReportSent;

    [ObservableProperty]
    private string traceId;

    [ObservableProperty]
    private string exceptionMessage;

    [ObservableProperty]
    private string stackTrace;

    [ObservableProperty]
    private string userComment = string.Empty;

    [ObservableProperty]
    private bool isSendingReport;

    public ExceptionReportViewModel(SentryId eventId, Exception exception)
    {
        associatedEventId = eventId;
        TraceId = $"trace.id: {eventId}";
        ExceptionMessage = exception.Message;
        StackTrace = exception.ToString();
    }

    [RelayCommand]
    private async Task SendReportAsync()
    {
        if (IsSendingReport)
            return;

        IsSendingReport = true;

        try
        {
            // Send feedback if comment is provided
            if (!string.IsNullOrWhiteSpace(UserComment))
            {
                // Note: v2rayWinUI may not have user email, so omit it
                SentrySdk.CaptureFeedback(UserComment, contactEmail: null, associatedEventId: associatedEventId);
            }

            // Flush to ensure feedback is sent
            await SentrySdk.FlushAsync();
        }
        catch (Exception ex)
        {
            Logging.SaveLog($"Failed to send feedback: {ex}");
        }
        finally
        {
            IsSendingReport = false;
            // ReportSent?.Invoke();
        }
    }
}