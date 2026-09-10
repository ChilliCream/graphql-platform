namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

internal static class BoardIdentity
{
    /// <summary>
    /// The toast shown instead of any write, on every tab, when the board
    /// has no actor. It names the cause rather than a remedy: an identity is
    /// bound by the Nitro hooks when a coding session starts, so there is no
    /// command a plain shell can run to take one.
    /// </summary>
    public const string NoIdentityMessage =
        "No agent identity for this session, so the board is read-only. "
        + "Nitro binds one when a hooked coding session starts.";
}
