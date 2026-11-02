using Microsoft.AspNetCore.Http;
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

    protected GraphQLServerOptions GetOptions(HttpContext context)
    {
        if (_options is not null)
        {
            return _options;
        }

        var optionOverrides = context.GetEndpoint()?.Metadata.GetOrderedMetadata<GraphQLServerOptionsOverride>();

        if (optionOverrides is { Count: > 0 })
        {
            var options = _baseOptions.Clone();

            foreach (var optionsOverride in optionOverrides)
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
