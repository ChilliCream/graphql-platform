namespace HotChocolate.Execution;

/// <summary>
/// Extensions methods for <see cref="OperationRequestBuilder"/>.
/// </summary>
public static class OperationResultBuilderExtensions
{
    /// <summary>
    /// Registers a cleanup task for execution resources with the <see cref="OperationResultBuilder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="OperationResultBuilder"/>.
    /// </param>
    /// <param name="clean">
    /// A cleanup task that will be executed when this result is disposed.
    /// </param>
    public static void RegisterForCleanup(this OperationResultBuilder builder, Action clean)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(clean);

        builder.RegisterForCleanup(() =>
        {
            clean();
            return default;
        });
    }

    /// <summary>
    /// Registers a cleanup task for execution resources with the <see cref="OperationResultBuilder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="OperationResultBuilder"/>.
    /// </param>
    /// <param name="disposable">
    /// The resource that needs to be disposed.
    /// </param>
    public static void RegisterForCleanup(this OperationResultBuilder builder, IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(disposable);

        builder.RegisterForCleanup(disposable.Dispose);
    }

    /// <summary>
    /// Registers a cleanup task for execution resources with the <see cref="OperationResultBuilder"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IExecutionResult"/>.
    /// </param>
    /// <param name="disposable">
    /// The resource that needs to be disposed.
    /// </param>
    public static void RegisterForCleanup(
        this OperationResultBuilder builder,
        IAsyncDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(disposable);

        builder.RegisterForCleanup(disposable.DisposeAsync);
    }
}
