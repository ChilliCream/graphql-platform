namespace HotChocolate.Execution;

/// <summary>
/// Represents the request-specific overrides for the persisted operation feature.
/// </summary>
/// <param name="AllowNonPersistedOperation">
/// If <c>true</c>, the request allows non-persisted operations.
/// </param>
public sealed record PersistedOperationRequestOverrides(
    bool AllowNonPersistedOperation = false);
