namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Constants shared by Nitro's opencode prompt delivery and hook shim.
/// </summary>
internal static class OpencodeHookProtocol
{
    /// <summary>
    /// The namespaced metadata key that marks a prompt pushed by Nitro.
    /// </summary>
    public const string PushedPromptMetadataKey = "com.chillicream.nitro.pushed";

    /// <summary>
    /// The exact metadata value that marks a prompt pushed by Nitro.
    /// </summary>
    public const string PushedPromptMetadataValue = "true";
}
