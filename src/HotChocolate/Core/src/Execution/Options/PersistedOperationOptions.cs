namespace HotChocolate.Execution.Options;

/// <summary>
/// Represents the options to configure the
/// behavior of the persisted operation middleware.
/// </summary>
[Flags]
public enum PersistedOperationOptions
{
    /// <summary>
    /// Nothing is configured.
    /// </summary>
    None = 0,

    /// <summary>
    /// Only persisted operations are allowed.
    /// </summary>
    OnlyPersistedOperations = 1,

    /// <summary>
    /// Allow standard GraphQL requests if the GraphQL document
    /// match a persisted operation document.
    /// </summary>
    MatchStandardDocument = 2,

    /// <summary>
    /// Skip validation for persisted operations documents.
    /// </summary>
    SkipValidationForPersistedDocument = 4
}
