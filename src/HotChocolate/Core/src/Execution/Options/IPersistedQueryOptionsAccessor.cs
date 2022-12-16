namespace HotChocolate.Execution.Options;

/// <summary>
/// Provides access to the persisted query options.
/// </summary>
public interface IPersistedQueryOptionsAccessor
{
    /// <summary>
    /// Specifies if only persisted queries are allowed when using
    /// the persisted query pipeline.
    /// </summary>
    bool OnlyAllowPersistedQueries { get; }

    /// <summary>
    /// The error that will be thrown when only persisted
    /// queries are allowed and a normal query is issued.
    /// </summary>
    IError OnlyPersistedQueriesAreAllowedError { get; }
}
