using HotChocolate.Resolvers;

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Represents a batch middleware configuration.
/// </summary>
public sealed class BatchFieldMiddlewareConfiguration : IRepeatableConfiguration
{
    /// <summary>
    /// Initializes a new instance of <see cref="BatchFieldMiddlewareConfiguration"/>.
    /// </summary>
    /// <param name="middleware">
    /// The delegate representing the batch middleware.
    /// </param>
    /// <param name="isRepeatable">
    /// Defines if the middleware is repeatable and
    /// the same middleware is allowed to occur multiple times.
    /// </param>
    /// <param name="key">
    /// The key is optional and is used to identify a middleware.
    /// </param>
    public BatchFieldMiddlewareConfiguration(
        BatchFieldMiddleware middleware,
        bool isRepeatable = true,
        string? key = null)
    {
        Middleware = middleware;
        IsRepeatable = isRepeatable;
        Key = key;
    }

    /// <summary>
    /// Gets the delegate representing the batch middleware.
    /// </summary>
    public BatchFieldMiddleware Middleware { get; }

    /// <summary>
    /// Defines if the middleware is repeatable and
    /// the same middleware is allowed to occur multiple times.
    /// </summary>
    public bool IsRepeatable { get; }

    /// <summary>
    /// The key is optional and is used to identify a middleware.
    /// </summary>
    public string? Key { get; }
}
