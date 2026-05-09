using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

/// <summary>
/// The Hot Chocolate ASP.NET core middleware base class.
/// </summary>
public abstract class MiddlewareBase : IDisposable
{
    private readonly RequestDelegate _next;
    private readonly GraphQLServerOptions _baseOptions;
    private GraphQLServerOptions? _options;
    private bool _disposed;

    protected MiddlewareBase(
        RequestDelegate next,
        HttpRequestExecutorProxy executor,
        GraphQLServerOptions baseOptions)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(baseOptions);

        _next = next;
        Executor = executor;
        _baseOptions = baseOptions;
    }

    /// <summary>
    /// Gets the request executor proxy.
    /// </summary>
    protected HttpRequestExecutorProxy Executor { get; }

    /// <summary>
    /// Invokes the next middleware in line.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    protected Task NextAsync(HttpContext context) => _next(context);

    /// <summary>
    /// Gets the executor session for the current request.
    /// If a schema name was resolved dynamically via
    /// <see cref="DynamicSchemaMiddleware"/>, the session for that
    /// schema is returned. Otherwise, the session for the default
    /// schema bound to this middleware is returned.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The executor session.</returns>
    protected async ValueTask<ExecutorSession> GetExecutorSessionAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (DynamicSchemaMiddleware.TryGetSchemaName(context, out var schemaName))
        {
            var executorProvider = context.RequestServices.GetRequiredService<IRequestExecutorProvider>();
            var executor = await executorProvider.GetExecutorAsync(schemaName, cancellationToken);
            return new ExecutorSession(executor);
        }

        return await Executor.GetOrCreateSessionAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the server options for the current request.
    /// If a schema name was resolved dynamically, the options for
    /// that schema are returned. Otherwise, the options for the
    /// default schema bound to this middleware are returned.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>The server options.</returns>
    protected GraphQLServerOptions GetOptions(HttpContext context)
    {
        if (DynamicSchemaMiddleware.TryGetSchemaName(context, out var schemaName))
        {
            var optionsMonitor = context.RequestServices.GetRequiredService<IOptionsMonitor<GraphQLServerOptions>>();
            var schemaOptions = optionsMonitor.Get(schemaName);

            var optionOverrides = context.GetEndpoint()?.Metadata.GetOrderedMetadata<GraphQLServerOptionsOverride>();

            if (optionOverrides is { Count: > 0 })
            {
                var options = schemaOptions.Clone();

                foreach (var optionsOverride in optionOverrides)
                {
                    optionsOverride.Apply(options);
                }

                return options;
            }

            return schemaOptions;
        }

        if (_options is not null)
        {
            return _options;
        }

        var endpointOverrides = context.GetEndpoint()?.Metadata.GetOrderedMetadata<GraphQLServerOptionsOverride>();

        if (endpointOverrides is { Count: > 0 })
        {
            var options = _baseOptions.Clone();

            foreach (var optionsOverride in endpointOverrides)
            {
                optionsOverride.Apply(options);
            }

            _options = options;
            return options;
        }

        _options = _baseOptions;
        return _options;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            Executor.Dispose();
            _disposed = true;
        }
    }
}
