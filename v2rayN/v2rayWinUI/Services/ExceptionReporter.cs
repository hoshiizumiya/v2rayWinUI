using Sentry;
using ServiceLib.Common;
using System;

namespace v2rayWinUI.Services;

internal interface IExceptionReporter
{
    void Report(Exception exception, string context);
}

internal sealed class ExceptionReporter : IExceptionReporter
{
    public void Report(Exception exception, string context)
    {
        try
        {
            Logging.SaveLog(context, exception);
        }
        catch { }

        try
        {
            SentrySdk.CaptureException(exception);
        }
        catch { }
    }
}
