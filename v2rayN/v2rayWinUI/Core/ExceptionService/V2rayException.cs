// Copyright (c) v2rayWinUI Contributors. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace v2rayWinUI.Core.ExceptionService;

/// <summary>
/// Custom exception for v2rayWinUI
/// Follows SnapHutao's exception handling pattern
/// </summary>
internal sealed class V2rayException : Exception
{
    [StackTraceHidden]
    public V2rayException(string message, Exception? innerException = default)
        : base($"{message}\n{innerException?.Message}", innerException)
    {
    }

    [StackTraceHidden]
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static V2rayException Throw(string message, Exception? innerException = default)
    {
        throw new V2rayException(message, innerException);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowIf([DoesNotReturnIf(true)] bool condition, string message, Exception? innerException = default)
    {
        if (condition)
        {
            throw new V2rayException(message, innerException);
        }
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowIfNot([DoesNotReturnIf(false)] bool condition, string message, Exception? innerException = default)
    {
        if (!condition)
        {
            throw new V2rayException(message, innerException);
        }
    }

    [StackTraceHidden]
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InvalidOperationException InvalidOperation(string message, Exception? innerException = default)
    {
        throw new InvalidOperationException(message, innerException);
    }
}
