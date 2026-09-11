namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Constants shared by Nitro's opencode prompt delivery and hook shim.
/// </summary>
internal static class OpencodeHookProtocol
{
    /// <summary>
    /// The reserved text prefix Nitro's HTTP push prepends to a message it
    /// delivers directly into an opencode session (the push side, see the
    /// wake/ping dispatcher). The generated shim strips this exact prefix
    /// from the delivered text part before the model ever sees it, and
    /// marks the hook payload <c>nitroPushed</c> for the turn so the
    /// handler skips rearm and re-injection.
    /// </summary>
    public const string PushedPromptPrefix = "[[nitro:pushed]] ";
}
