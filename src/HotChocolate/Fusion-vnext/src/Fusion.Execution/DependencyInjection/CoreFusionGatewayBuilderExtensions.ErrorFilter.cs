using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Adds an error filter delegate.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="errorFilter">
    /// A delegate that is called for each error and can modify or replace it.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddErrorFilter(
        this IFusionGatewayBuilder builder,
        Func<IError, IError> errorFilter)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(errorFilter);

        return builder.ConfigureSchemaServices(
            (_, s) => s.AddSingleton<IErrorFilter>(new FuncErrorFilterWrapper(errorFilter)));
    }

    /// <summary>
    /// Adds an error filter.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="IErrorFilter"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <typeparamref name="T"/> will be activated with the <see cref="IServiceProvider"/> of the schema services.
    /// If your <typeparamref name="T"/> needs to access application services you need to
    /// make the services available in the schema services via <see cref="AddApplicationService"/>.
    /// </remarks>
    public static IFusionGatewayBuilder AddErrorFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IFusionGatewayBuilder builder)
        where T : class, IErrorFilter
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<T>();
        return builder.ConfigureSchemaServices(
            (sp, s) => s.AddSingleton<IErrorFilter, T>(_ => sp.GetRequiredService<T>()));
    }

    /// <summary>
    /// Adds an error filter.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IFusionGatewayBuilder"/>.
    /// </param>
    /// <param name="factory">
    /// A factory that creates the error filter instance.
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="IErrorFilter"/> implementation.
    /// </typeparam>
    /// <returns>
    /// Returns the <see cref="IFusionGatewayBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> passed to the <paramref name="factory"/>
    /// is for the schema services. If you need to access application services
    /// you need to either make the services available in the schema services
    /// via <see cref="AddApplicationService"/> or use
    /// <see cref="ExecutionServiceProviderExtensions.GetRootServiceProvider(IServiceProvider)"/>
    /// to access the application services from within the schema service provider.
    /// </remarks>
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
