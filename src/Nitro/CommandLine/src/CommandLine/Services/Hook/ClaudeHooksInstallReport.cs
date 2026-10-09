namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record ClaudeHooksInstallReport(string SettingsPath, IReadOnlyList<HookInstallEventResult> Events);
