namespace HotChocolate.Execution.Pipeline;

/// <summary>
/// Provides common middleware configurations.
/// </summary>
public static class CommonMiddleware
{
    /// <summary>
    /// Gets the key for the instrumentation middleware.
    /// </summary>
    public static string InstrumentationKey => nameof(InstrumentationMiddleware);

    /// <summary>
    /// Gets the middleware configuration for wrapping
    /// the request execution in an instrumentation scope.
    /// </summary>
    public static RequestMiddlewareConfiguration Instrumentation
        => InstrumentationMiddleware.Create();

    /// <summary>
    /// Gets the key for the exception middleware.
    /// </summary>
    public static string UnhandledExceptionsKey => nameof(ExceptionMiddleware);

    /// <summary>
    /// Gets the middleware configuration for catching unhandled exceptions.
    /// </summary>
    public static RequestMiddlewareConfiguration UnhandledExceptions
        => ExceptionMiddleware.Create();

    /// <summary>
    /// Gets the key for the document cache middleware.
    /// </summary>
    public static string DocumentCacheKey => nameof(DocumentCacheMiddleware);

    /// <summary>
    /// Gets the middleware configuration for caching GraphQL operation documents.
    /// </summary>
    public static RequestMiddlewareConfiguration DocumentCache
        => DocumentCacheMiddleware.Create();

    public static string DocumentParserKey => nameof(DocumentParserMiddleware);

    /// <summary>
    /// Gets the middleware configuration for parsing GraphQL operation documents.
    /// </summary>
    public static RequestMiddlewareConfiguration DocumentParser
        => DocumentParserMiddleware.Create();

    /// <summary>
    /// Gets the key for the document validation middleware.
    /// </summary>
    public static string DocumentValidationKey => nameof(DocumentValidationMiddleware);

    /// <summary>
    /// Gets the middleware configuration for validating GraphQL operation documents.
    /// </summary>
    public static RequestMiddlewareConfiguration DocumentValidation
        => DocumentValidationMiddleware.Create();

    /// <summary>
    /// Gets the key for the cost analyzer middleware.
    /// </summary>
    public static string SkipWarmupExecutionKey => nameof(SkipWarmupExecutionMiddleware);

    /// <summary>
    /// Gets the middleware configuration for skipping the actual operation execution when executing a warmup request.
    /// </summary>
    public static RequestMiddlewareConfiguration SkipWarmupExecution
        => SkipWarmupExecutionMiddleware.Create();
}
