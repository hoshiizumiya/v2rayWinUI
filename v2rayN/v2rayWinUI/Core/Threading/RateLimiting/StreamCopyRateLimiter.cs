// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using System.Threading.RateLimiting;

namespace v2rayWinUI.Core.Threading.RateLimiting;

internal static class StreamCopyRateLimiter
{
    private const double ReplenishmentCountPerSecond = 20;

    public static TokenBucketRateLimiter? Create(int bytesPerSecond = 0)
    {
        return PrivateCreate(bytesPerSecond);
    }

    private static TokenBucketRateLimiter? PrivateCreate(int bytesPerSecond)
    {

        if (bytesPerSecond <= 0)
        {
            return default;
        }

        TokenBucketRateLimiterOptions options = new()
        {
            TokenLimit = bytesPerSecond,
            ReplenishmentPeriod = TimeSpan.FromMilliseconds(1000 / ReplenishmentCountPerSecond),
            TokensPerPeriod = (int)(bytesPerSecond / ReplenishmentCountPerSecond),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        };

        return new(options);
    }
}
