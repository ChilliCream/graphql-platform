using HotChocolate.PersistedOperations;

namespace HotChocolate.Execution.Options;

/// <summary>
/// Provides access to the persisted operation options.
/// </summary>
public interface IPersistedOperationOptionsAccessor
{
    /// <summary>
    /// Gets the persisted operation options.
    /// </summary>
    PersistedOperationOptions PersistedOperations { get; }
}
