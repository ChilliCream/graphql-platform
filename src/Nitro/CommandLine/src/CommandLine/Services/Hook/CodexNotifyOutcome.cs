namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What <see cref="ICodexHookHandler.HandleNotifyAsync"/> decided: whether a digest
/// was actually queued via <c>codex queue</c>.
/// </summary>
internal sealed record CodexNotifyOutcome
{
    public static readonly CodexNotifyOutcome Neutral = new();

    public bool Queued { get; init; }
}
