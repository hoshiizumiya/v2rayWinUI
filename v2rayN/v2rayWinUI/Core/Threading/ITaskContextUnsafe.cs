// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Dispatching;

namespace v2rayWinUI.Core.Threading;

internal interface ITaskContextUnsafe
{
    DispatcherQueue DispatcherQueue { get; }
}
