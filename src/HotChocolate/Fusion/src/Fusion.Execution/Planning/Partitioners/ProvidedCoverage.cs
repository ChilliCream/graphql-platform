namespace HotChocolate.Fusion.Planning.Partitioners;

/// <summary>
/// Describes how much of the data available at a level a provided selection set accounts for.
/// </summary>
internal enum ProvidedCoverage
{
    /// <summary>
    /// The provided set is additive. It accounts for some fields, and the current schema still
    /// natively resolves the rest. This is how a federation <c>@provides</c> scope behaves, a
    /// load optimization layered on native ownership.
    /// </summary>
    Partial,

    /// <summary>
    /// The provided set is the complete data already available at this level, such as an event
    /// stream message payload. Fields it does not cover are not available here and spill to a
    /// follow-up lookup.
    /// </summary>
    Complete
}
