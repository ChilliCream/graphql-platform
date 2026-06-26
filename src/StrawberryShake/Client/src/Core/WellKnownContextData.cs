namespace StrawberryShake;

/// <summary>
/// Well known keys for <see cref="IOperationResult.ContextData"/>.
/// </summary>
public static class WellKnownContextData
{
    /// <summary>
    /// The key under which the raw transport "data" payload of an operation result is
    /// captured. This allows the payload to be persisted (for example via Blazor's
    /// <c>PersistentComponentState</c>) and later rehydrated without re-executing the
    /// operation.
    /// </summary>
    public const string PersistedData = "StrawberryShake.PersistedData";
}
