namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The opencode shim response envelope containing chat parts to append.
/// </summary>
internal sealed record OpencodeHookResponse
{
    /// <summary>
    /// Text parts to append to the chat output, or null for no changes.
    /// </summary>
    public IReadOnlyList<string>? Parts { get; init; }
}
