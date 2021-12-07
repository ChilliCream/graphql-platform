using HotChocolate.Resolvers;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

/// <summary>
/// Represents a middleware configuration.
/// </summary>
public sealed class FieldMiddlewareDefinition : IMiddlewareDefinition
{
    /// <summary>
    /// Initializes a new instance of <see cref="FieldMiddlewareDefinition"/>.
    /// </summary>
    /// <param name="middleware">
    /// The delegate representing the middleware.
    /// </param>
    /// <param name="isRepeatable">
    /// Defines if the middleware or result converters is repeatable and
    /// the same middleware is allowed to occur multiple times.
    /// </param>
    /// <param name="key">
    /// The key is optional and is used to identify a middleware.
    /// </param>
    public FieldMiddlewareDefinition(
        FieldMiddleware middleware,
        bool isRepeatable = true,
        string? key = null)
    {
        Middleware = middleware;
        IsRepeatable = isRepeatable;
        Key = key;
    }

    /// <summary>
    /// Gets the delegate representing the middleware.
    /// </summary>
    public FieldMiddleware Middleware { get; }

    /// <summary>
    /// Defines if the middleware or result converters is repeatable and
    /// the same middleware is allowed to be occur multiple times.
    /// </summary>
    public bool IsRepeatable { get; }

    /// <summary>
    /// The key is optional and is used to identify a middleware.
    /// </summary>
    public string? Key { get; }
}
