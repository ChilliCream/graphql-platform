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
    bool OnlyAllowPersistedOperations { get; }

    /// <summary>
    /// The error that will be thrown when only persisted
    /// operations are allowed and a normal operation is issued.
    /// </summary>
    IError OnlyPersistedOperationsAreAllowedError { get; }
}
