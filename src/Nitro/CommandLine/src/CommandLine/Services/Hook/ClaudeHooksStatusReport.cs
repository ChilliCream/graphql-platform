namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record ClaudeHooksStatusReport(string SettingsPath, IReadOnlyList<HookStatusEventResult> Events);
