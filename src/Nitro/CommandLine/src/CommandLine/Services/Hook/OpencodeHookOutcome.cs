namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The text parts an opencode hook appends to a chat response.
/// </summary>
internal sealed record OpencodeHookOutcome
{
    /// <summary>
    /// A neutral result that appends no parts.
    /// </summary>
    public static readonly OpencodeHookOutcome Neutral = new();

    /// <summary>
    /// Text parts for the shim to append to the current chat response.
    /// </summary>
    public IReadOnlyList<string> Parts { get; init; } = [];
}
