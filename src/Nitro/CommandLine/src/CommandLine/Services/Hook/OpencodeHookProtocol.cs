namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Constants shared by Nitro's opencode prompt delivery and hook shim.
/// </summary>
internal static class OpencodeHookProtocol
{
    /// <summary>
    /// The reserved text prefix Nitro's HTTP push prepends to a message it
    /// delivers directly into an opencode session.
    /// </summary>
    public const string PushedPromptPrefix = "[[nitro:pushed]] ";
}
