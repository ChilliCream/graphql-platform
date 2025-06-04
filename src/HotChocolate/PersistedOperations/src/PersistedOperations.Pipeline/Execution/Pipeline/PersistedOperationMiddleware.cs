using HotChocolate.PersistedOperations;

namespace HotChocolate.Execution.Pipeline;

/// <summary>
/// Provides middleware configurations for persisted operations.
/// </summary>
public static class PersistedOperationMiddleware
{
    /// <summary>
    /// Gets the middleware configuration for writing persisted operations
    /// to the <see cref="IOperationDocumentStorage"/>.
    /// </summary>
    public static RequestMiddlewareConfiguration WritePersistedOperation
        => WritePersistedOperationMiddleware.Create();

    /// <summary>
    /// Gets the middleware configuration for reading persisted operations
    /// from the <see cref="IOperationDocumentStorage"/>.
    /// </summary>
    public static RequestMiddlewareConfiguration ReadPersistedOperation
        => ReadPersistedOperationMiddleware.Create();

    /// <summary>
    /// Gets the middleware configuration for handling persisted operations
    /// that are not found in the <see cref="IOperationDocumentStorage"/>.
    /// </summary>
    public static RequestMiddlewareConfiguration PersistedOperationNotFound
        => PersistedOperationNotFoundMiddleware.Create();

    /// <summary>
    /// Gets the middleware configuration for ensuring that only persisted operations are allowed to execute.
    /// </summary>
    public static RequestMiddlewareConfiguration OnlyPersistedOperationsAllowed
        => OnlyPersistedOperationsAllowedMiddleware.Create();

    /// <summary>
    /// Gets the middleware configuration for automatically handling persisted operation not found errors.
    /// </summary>
    public static RequestMiddlewareConfiguration AutomaticPersistedOperationNotFound
        => AutomaticPersistedOperationNotFoundMiddleware.Create();
}
