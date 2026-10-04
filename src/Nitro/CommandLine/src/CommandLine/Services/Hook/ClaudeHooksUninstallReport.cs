namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record ClaudeHooksUninstallReport(string SettingsPath, IReadOnlyList<HookUninstallEventResult> Events);
