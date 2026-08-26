namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What <see cref="ICodexHookHandler.HandleNotifyAsync"/> decided: whether a
/// digest was actually queued via <c>codex queue</c>. Purely observational
/// (the notify command's stdout/stderr and its own exit code are not part of
/// Codex's notify contract the way <see cref="CodexHookOutcome"/> is part of
/// the hooks.json contract) - this exists so tests, and the command layer's
/// own diagnostics, can tell whether the reserve-then-emit path actually ran.
/// </summary>
internal sealed record CodexNotifyOutcome
{
    public static readonly CodexNotifyOutcome Neutral = new();

    public bool Queued { get; init; }
}
