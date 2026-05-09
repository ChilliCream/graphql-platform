using Microsoft.AspNetCore.Http;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore;

/// <summary>
/// A middleware that resolves the schema name for the current request
/// using a delegate and stores it in <see cref="HttpContext.Items"/>
/// for downstream middleware to use.
/// </summary>
public sealed class DynamicSchemaMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Func<HttpContext, string> _schemaNameResolver;

    internal static readonly object SchemaNameKey = new();

    /// <summary>
    /// Creates a new instance of <see cref="DynamicSchemaMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="schemaNameResolver">
    /// A delegate that resolves the schema name from the current <see cref="HttpContext"/>.
    /// </param>
    public DynamicSchemaMiddleware(
        RequestDelegate next,
        Func<HttpContext, string> schemaNameResolver)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(schemaNameResolver);

        _next = next;
        _schemaNameResolver = schemaNameResolver;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var schemaName = _schemaNameResolver(context);
        context.Items[SchemaNameKey] = schemaName;
        await _next(context);
    }

    /// <summary>
    /// Attempts to get the schema name that was resolved by
    /// <see cref="DynamicSchemaMiddleware"/> for the current request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="schemaName">
    /// The resolved schema name, or <c>null</c> if no schema name was resolved.
    /// </param>
    /// <returns>
    /// <c>true</c> if a schema name was resolved for the current request;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetSchemaName(HttpContext context, out string? schemaName)
    {
        if (context.Items.TryGetValue(SchemaNameKey, out var value) && value is string s)
        {
            schemaName = s;
            return true;
        }

        schemaName = null;
        return false;
    }
}
