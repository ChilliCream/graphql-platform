namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Whether a requirement read without the unavailable data takes data from a call of the access path.
/// </summary>
internal enum ArrivalCallState
{
    /// <summary>
    /// The call is not read.
    /// </summary>
    None,

    /// <summary>
    /// The call is read.
    /// </summary>
    Readable,

    /// <summary>
    /// The call depends on the call that resolves the requiring field while the unavailable schema serves every
    /// field below it.
    /// </summary>
    Pending
}
