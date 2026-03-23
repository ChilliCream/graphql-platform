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
    /// Appends a middleware to the end of the pipeline.
    /// </summary>
    IMediatorBuilder Use(MediatorMiddlewareConfiguration middleware);

    /// <summary>
    /// Inserts a middleware after the middleware identified by <paramref name="after"/>.
    /// If no middleware with that key is found, the middleware is appended to the end.
    /// </summary>
    IMediatorBuilder Append(string after, MediatorMiddlewareConfiguration middleware);

    /// <summary>
    /// Inserts a middleware at the beginning of the pipeline.
    /// </summary>
    IMediatorBuilder Prepend(MediatorMiddlewareConfiguration middleware);

    /// <summary>
    /// Inserts a middleware before the middleware identified by <paramref name="before"/>.
    /// If no middleware with that key is found, the middleware is prepended to the beginning.
    /// </summary>
    IMediatorBuilder Prepend(string before, MediatorMiddlewareConfiguration middleware);

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
    /// Registers a pipeline configuration for the specified message type.
    /// This method is intended for use by source-generated code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    void RegisterPipeline(MediatorPipelineConfiguration configuration);
}
