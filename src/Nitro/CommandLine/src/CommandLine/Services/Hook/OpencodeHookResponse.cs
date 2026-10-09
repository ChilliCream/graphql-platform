namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The opencode shim response containing context parts to append to the current prompt.
/// </summary>
internal sealed record OpencodeHookResponse
{
    /// <summary>
    /// Text parts to append to the current prompt, or null for no changes.
    /// </summary>
    public IReadOnlyList<string>? Parts { get; init; }
}
