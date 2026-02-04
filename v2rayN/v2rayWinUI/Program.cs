using Microsoft.UI.Xaml;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Sentry;
using v2rayWinUI.Helpers;

namespace v2rayWinUI;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Must be set before any ReactiveUI pipeline runs.
            ObservableExceptionHandler.Initialize();
        }
        catch
        {
        }

        try
        {
            SentrySdk.Init(options =>
            {
                options.Dsn = "https://e7f2628d83a3d421d979abcd2e86cb5b@o4510805000454144.ingest.de.sentry.io/4510805007269968";
                options.Debug = false;
                options.AutoSessionTracking = true;
                options.IsGlobalModeEnabled = true;
                options.TracesSampleRate = 0.1;
                options.AttachStacktrace = true;
                options.SendDefaultPii = false;
                options.MaxBreadcrumbs = 100;

                options.SetBeforeSend((sentryEvent, hint) =>
                {
                    try
                    {
                        sentryEvent.SetTag("app", "v2rayWinUI");
                        sentryEvent.SetTag("version", ServiceLib.Common.Utils.GetVersion());
                    }
                    catch { }
                    return sentryEvent;
                });
            });

            SentrySdk.AddBreadcrumb("Application starting", "lifecycle");

            ObservableExceptionHandler.ExceptionCaptured += static (_, ex) =>
            {
                try
                {
                    ex.SetSentryMechanism("ReactiveUI", handled: true);
                    SentrySdk.CaptureException(ex);
                }
                catch
                {
                }
            };
        }
        catch
        {
        }

        global::Microsoft.UI.Xaml.Application.Start(_ => new App());
    }
}
