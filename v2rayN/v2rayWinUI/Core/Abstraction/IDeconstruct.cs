// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace v2rayWinUI.Core.Abstraction;

internal interface IDeconstruct<T1, T2>
{
    void Deconstruct(out T1 item1, out T2 item2);
}
