using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    public static IFusionGatewayBuilder AddErrorFilter(
        this IFusionGatewayBuilder builder,
        Func<IError, IError> errorFilter)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(errorFilter);

        return builder.ConfigureSchemaServices(
            (_, s) => s.AddSingleton<IErrorFilter>(new FuncErrorFilterWrapper(errorFilter)));
    }

    public static IFusionGatewayBuilder AddErrorFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IErrorFilter
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<T>();
        return builder.ConfigureSchemaServices(
            (sp, s) => s.AddSingleton<IErrorFilter, T>(_ => sp.GetRequiredService<T>()));
    }

    public static IFusionGatewayBuilder AddErrorFilter<T>(
        this IFusionGatewayBuilder builder,
        Func<IServiceProvider, T> factory)
        where T : class, IErrorFilter
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchemaServices(
            (sp, s) => s.AddSingleton<IErrorFilter, T>(_ => factory(sp)));
    }
}
