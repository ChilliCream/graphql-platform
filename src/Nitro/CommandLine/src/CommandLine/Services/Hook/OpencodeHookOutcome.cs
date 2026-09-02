namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The text parts an opencode hook appends to a chat response.
/// </summary>
internal sealed record OpencodeHookOutcome
{
    /// <summary>
    /// A neutral result that appends no parts and authorizes no idle delivery.
    /// </summary>
    public static readonly OpencodeHookOutcome Neutral = new();

    /// <summary>
    /// Text parts for the shim to append to the current chat response.
    /// </summary>
    public IReadOnlyList<string> Parts { get; init; } = [];

    /// <summary>
    /// The nudge reserved for the idle channel, or null when no push is due.
    /// </summary>
    public string? IdleDelivery { get; init; }
}
