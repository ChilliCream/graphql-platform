namespace HotChocolate.Execution;

/// <summary>
/// Provides extension methods for the persisted operation request overrides.
/// </summary>
public static class PersistedOperationRequestOverridesExtensions
{
    /// <summary>
    /// Allows non-persisted operations for the current request.
    /// </summary>
    /// <param name="builder">
    /// The operation request builder.
    /// </param>
    /// <returns>
    /// Returns the operation request builder.
    /// </returns>
    public static OperationRequestBuilder AllowNonPersistedOperation(
        this OperationRequestBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = builder.Features.Get<PersistedOperationRequestOverrides>();

        if (options is null)
        {
            options = new PersistedOperationRequestOverrides(AllowNonPersistedOperation: true);
        }
        else
        {
            options = new PersistedOperationRequestOverrides(AllowNonPersistedOperation: true);
        }

        builder.Features.Set(options);
        return builder;
    }
}
