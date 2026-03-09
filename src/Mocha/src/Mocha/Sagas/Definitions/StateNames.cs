namespace Mocha.Sagas;

/// <summary>
/// Well-known state names used by the saga state machine infrastructure.
/// </summary>
public static class StateNames
{
    /// <summary>
    /// The reserved name for the catch-all state that defines transitions applying to all non-initial and non-final states.
    /// </summary>
    public const string DuringAny = "__DuringAny";

    /// <summary>
    /// The reserved name for the initial state from which new saga instances begin.
    /// </summary>
    public const string Initial = "__Initial";

    /// <summary>
    /// The reserved name for the timed-out final state.
    /// </summary>
    public const string TimedOut = "__TimedOut";
}
