using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator;

/// <summary>
/// Defines the contract for configuring a Mocha Mediator pipeline.
/// This is the internal builder interface used by deferred configuration actions.
/// </summary>
public interface IMediatorBuilder
{
    /// <summary>
    /// Configures the <see cref="MediatorOptions"/> for this mediator instance.
    /// </summary>
    IMediatorBuilder ConfigureOptions(Action<MediatorOptions> configure);

    /// <summary>
    /// Adds a middleware to the pipeline. When neither <paramref name="before"/> nor <paramref name="after"/>
    /// is specified the middleware is appended to the end of the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware configuration.</param>
    /// <param name="before">If specified, the middleware is inserted before the middleware with the given key.</param>
    /// <param name="after">If specified, the middleware is inserted after the middleware with the given key.</param>
    IMediatorBuilder Use(MediatorMiddlewareConfiguration middleware, string? before = null, string? after = null);

    /// <summary>
    /// Configures the mediator's feature collection.
    /// Features are available to middleware factories via <see cref="MediatorMiddlewareFactoryContext.Features"/>.
    /// </summary>
    IMediatorBuilder ConfigureFeature(Action<IFeatureCollection> configure);

    /// <summary>
    /// Registers additional services into the mediator's internal service collection.
    /// </summary>
    IMediatorBuilder ConfigureServices(Action<IServiceCollection> configure);

    /// <summary>
    /// Registers additional services into the mediator's internal service collection,
    /// with access to the application-level service provider.
    /// </summary>
    IMediatorBuilder ConfigureServices(Action<IServiceProvider, IServiceCollection> configure);

    /// <summary>
    /// Registers a handler via descriptor-based configuration.
    /// </summary>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <param name="configure">An optional action to configure the handler descriptor.</param>
    void AddHandler<THandler>(Action<IMediatorHandlerDescriptor>? configure = null) where THandler : class;

    /// <summary>
    /// Registers a handler with a pre-built configuration.
    /// This method is intended for use by source-generated code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    void AddHandlerConfiguration(MediatorHandlerConfiguration configuration);
}
