using Sentry;

namespace v2rayWinUI.Core.ExceptionService;

/// <summary>
/// 捕获的异常信息，用于在异常处理管道中传递
/// </summary>
public readonly struct CapturedException
{
    /// <summary>
    /// Sentry事件ID，用于追踪和关联
    /// </summary>
    public readonly SentryId Id;

    /// <summary>
    /// 原始异常对象
    /// </summary>
    public readonly Exception Exception;

    public CapturedException(SentryId id, Exception exception)
    {
        Id = id;
        Exception = exception;
    }
}