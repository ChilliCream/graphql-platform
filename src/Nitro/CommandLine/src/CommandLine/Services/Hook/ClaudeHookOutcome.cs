namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What a Claude hook event handler decided to tell the harness, harness-
/// agnostic: the command layer is the only place that knows the wire shape
/// (<c>hookSpecificOutput.additionalContext</c> or
/// <c>{decision, reason}</c>) this maps to.
/// </summary>
internal sealed record ClaudeHookOutcome
{
    /// <summary>
    /// The neutral outcome every fail-open path and every event with
    /// nothing to say returns: no context to inject, no block.
    /// </summary>
    public static readonly ClaudeHookOutcome Neutral = new();

    /// <summary>
    /// Text to inject as additional context, or null for none. Never set
    /// together with <see cref="Block"/>.
    /// </summary>
    public string? AdditionalContext { get; init; }

    /// <summary>
    /// True to block the harness's Stop event (Claude: <c>decision: block</c>).
    /// </summary>
    public bool Block { get; init; }

    /// <summary>
    /// The reason surfaced alongside <see cref="Block"/>. Ignored when
    /// <see cref="Block"/> is false.
    /// </summary>
    public string? BlockReason { get; init; }
}
