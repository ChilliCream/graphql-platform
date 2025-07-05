using Microsoft.AspNetCore.Http;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

/// <summary>
/// The Hot Chocolate ASP.NET core middleware base class.
/// </summary>
public abstract class MiddlewareBase : IDisposable
{
    private readonly RequestDelegate _next;
    private GraphQLServerOptions? _options;
    private bool _disposed;

    protected MiddlewareBase(
        RequestDelegate next,
        HttpRequestExecutorProxy executor)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(executor);

        _next = next;
        Executor = executor;
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

        _options = context.GetGraphQLServerOptions() ?? new GraphQLServerOptions();
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
