namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Constants shared by Nitro's opencode prompt delivery and hook shim.
/// </summary>
internal static class OpencodeHookProtocol
{
    /// <summary>
    /// Prefixes prompts pushed by Nitro so the shim can identify and remove it before model delivery.
    /// </summary>
    public const string PushedPromptMarker = "[[nitro:pushed]] ";
}
