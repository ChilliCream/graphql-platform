namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// What an install did to each managed entry in <paramref name="SettingsPath"/>.
/// <paramref name="AllHooksDisabledIn"/> is the settings file whose <c>disableAllHooks</c>
/// flag keeps the harness from running any hook, or null when no such flag is set.
/// </summary>
internal sealed record ClaudeHooksInstallReport(
    string SettingsPath,
    IReadOnlyList<HookInstallEventResult> Events,
    string? AllHooksDisabledIn);
