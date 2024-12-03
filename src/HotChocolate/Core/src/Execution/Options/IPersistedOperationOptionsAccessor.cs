namespace HotChocolate.Execution.Options;

/// <summary>
/// Provides access to the persisted operation options.
/// </summary>
public interface IPersistedOperationOptionsAccessor
{
    /// <summary>
    /// Specifies the behavior of the persisted operation pipeline.
    /// </summary>
    PersistedOperationOptions PersistedOperations { get; }
}
