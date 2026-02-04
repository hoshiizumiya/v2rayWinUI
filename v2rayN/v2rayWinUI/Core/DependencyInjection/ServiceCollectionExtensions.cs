// Copyright (c) v2rayWinUI Contributors. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace v2rayWinUI.Core.DependencyInjection;

/// <summary>
/// Extension method for setting up v2rayWinUI services
/// Follows SnapHutao's service registration pattern
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddV2rayWinUICore(this IServiceCollection services)
    {
        // Exception handling
        services.AddSingleton<ExceptionService.ExceptionHandling>();

        return services;
    }
}
