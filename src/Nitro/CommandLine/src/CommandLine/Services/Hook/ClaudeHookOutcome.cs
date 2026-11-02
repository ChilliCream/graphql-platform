namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The context, Stop decision, or agent name returned by a Claude hook handler.
/// </summary>
internal sealed record ClaudeHookOutcome
{
    /// <summary>
    /// An outcome with no additional context, no Stop block, and no agent name.
    /// </summary>
    public static readonly ClaudeHookOutcome Neutral = new();

    /// <summary>
    /// Text to inject as additional context, or null for none.
    /// Ignored when <see cref="Block"/> is true.
    /// </summary>
    public string? AdditionalContext { get; init; }

    /// <summary>
    /// True to block the harness's Stop event.
    /// </summary>
    public bool Block { get; init; }

    /// <summary>
    /// The reason surfaced alongside <see cref="Block"/>. Ignored when
    /// <see cref="Block"/> is false.
    /// </summary>
    public string? BlockReason { get; init; }

    /// <summary>
    /// The name to give the agent an <c>AgentNaming</c> event names, or null for none.
    /// </summary>
    public string? AgentName { get; init; }
}
