namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The context parts an opencode hook returns for the current prompt.
/// </summary>
internal sealed record OpencodeHookOutcome
{
    /// <summary>
    /// A neutral result that appends no parts.
    /// </summary>
    public static readonly OpencodeHookOutcome Neutral = new();

    /// <summary>
    /// Text parts for the shim to append to the current prompt; an empty list adds no context.
    /// </summary>
    public IReadOnlyList<string> Parts { get; init; } = [];
}
