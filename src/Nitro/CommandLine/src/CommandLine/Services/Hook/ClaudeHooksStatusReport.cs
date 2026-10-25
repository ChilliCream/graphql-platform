namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The state of each managed entry in <paramref name="SettingsPath"/>.
/// <paramref name="AllHooksDisabledIn"/> is the settings file whose <c>disableAllHooks</c>
/// flag keeps the harness from running any hook, or null when no such flag is set.
/// </summary>
internal sealed record ClaudeHooksStatusReport(
    string SettingsPath,
    IReadOnlyList<HookStatusEventResult> Events,
    string? AllHooksDisabledIn);
