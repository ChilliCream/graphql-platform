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
    , IPersistedOperationOptionsAccessor
{
    /// <summary>
    /// Specifies that the transport is allowed to provide the schema SDL document as a file.
    /// </summary>
    bool EnableSchemaFileSupport { get; }
}
