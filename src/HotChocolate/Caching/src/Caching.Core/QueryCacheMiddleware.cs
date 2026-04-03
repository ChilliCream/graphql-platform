using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Caching;

/// <summary>
/// A shared request middleware that reads computed cache control constraints
/// from the operation's features and writes them to the result's context data.
/// The ASP.NET Core HTTP response formatter then translates these into
/// Cache-Control and Vary HTTP response headers.
/// </summary>
/// <remarks>
/// Both HotChocolate and Fusion store the <see cref="IOperation"/> on the
/// request context's feature collection, so this middleware reads it directly
/// from <c>context.Features.Get&lt;IOperation&gt;()</c>.
/// </remarks>
internal sealed class QueryCacheMiddleware
{
    private readonly ICacheControlOptions _options;
    private readonly RequestDelegate _next;

    private QueryCacheMiddleware(
        RequestDelegate next,
        ICacheControlOptionsAccessor optionsAccessor)
    {
        _next = next;
        _options = optionsAccessor.CacheControl;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        await _next(context).ConfigureAwait(false);

        if (!_options.Enable || context.ContextData.ContainsKey(SkipQueryCaching))
        {
            // If query caching is disabled or skipped,
            // we do not attempt to cache the result.
            return;
        }

        var operation = context.Features.Get<IOperation>();

        if (operation is null
            || !operation.Features.TryGet<CacheControlHeaderValue>(out var headerValue)
            || !operation.Features.TryGet<ImmutableCacheConstraints>(out var constraints))
        {
            return;
        }

        if (context.Result is OperationResult { Errors.Count: 0, ContextData: { } contextData } operationResult)
        {
            contextData = contextData.Add(ExecutionContextData.CacheControlHeaderValue, headerValue);

            if (constraints.Vary.Length > 0)
            {
                contextData = contextData.Add(ExecutionContextData.VaryHeaderValue, constraints.VaryString);
            }

            operationResult.ContextData = contextData;
        }
    }

    /// <summary>
    /// Creates a <see cref="RequestMiddlewareConfiguration"/> for the query cache middleware.
    /// </summary>
    internal static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var options = core.SchemaServices.GetRequiredService<ICacheControlOptionsAccessor>();
                var middleware = new QueryCacheMiddleware(next, options);
                return context => middleware.InvokeAsync(context);
            },
            WellKnownRequestMiddleware.QueryCacheMiddleware);
}
