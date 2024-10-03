namespace HotChocolate.Execution.Options;

/// <summary>
/// Provides access to the persisted operation options.
/// </summary>
public interface IPersistedOperationOptionsAccessor
{
    /// <summary>
    /// Specifies if only persisted operations are allowed when using
    /// the persisted operation pipeline.
    /// </summary>
    [Obsolete("Use PersistedOperationOptions instead.")]
    bool OnlyAllowPersistedOperations { get; }

    /// <summary>
    /// Specifies the behavior of the persisted operation middleware.
    /// </summary>
    PersistedOperationOptions PersistedOperationOptions { get; set; }

    /// <summary>
    /// The error that will be thrown when only persisted
    /// operations are allowed and a normal operation is issued.
    /// </summary>
    IError OnlyPersistedOperationsAreAllowedError { get; }
}
