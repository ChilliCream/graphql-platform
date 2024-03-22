namespace HotChocolate.Execution.Options;

/// <summary>
/// Represents the entirety of options accessors which are used to provide
/// components of the query execution engine access to settings, which were
/// provided from the outside, to influence the behavior of the query
/// execution engine itself.
/// </summary>
public interface IRequestExecutorOptionsAccessor
    : IErrorHandlerOptionsAccessor
        , IRequestTimeoutOptionsAccessor
        , IComplexityAnalyzerOptionsAccessor
        , IPersistedQueryOptionsAccessor
{
    /// <summary>
    /// Determine whether null-bubbling can be disabled on a per-request basis.
    /// </summary>
    bool AllowDisablingNullBubbling { get; }
}
