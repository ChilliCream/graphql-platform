using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618 // Both builder surfaces share the same configuration operations.

public static partial class CoreFusionRouterBuilderExtensions
{
    /// <summary>
    /// Adds a delegate that can modify or replace errors.
    /// </summary>
    public static IFusionRouterBuilder AddErrorFilter(
        this IFusionRouterBuilder builder,
        Func<IError, IError> errorFilter)
    {
        CoreFusionGatewayBuilderExtensions.AddErrorFilter(builder, errorFilter);
        return builder;
    }

    /// <summary>
    /// Adds an error filter resolved from the application services.
    /// </summary>
    public static IFusionRouterBuilder AddErrorFilter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionRouterBuilder builder)
        where T : class, IErrorFilter
    {
        CoreFusionGatewayBuilderExtensions.AddErrorFilter<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds an error filter created using the application services.
    /// </summary>
    public static IFusionRouterBuilder AddErrorFilter<T>(
        this IFusionRouterBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IErrorFilter
    {
        CoreFusionGatewayBuilderExtensions.AddErrorFilter(builder, factory);
        return builder;
    }
}
