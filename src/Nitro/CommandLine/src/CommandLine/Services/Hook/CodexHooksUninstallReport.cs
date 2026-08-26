namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record CodexHooksUninstallReport(
    string HooksJsonPath,
    IReadOnlyList<HookUninstallEventResult> HooksEvents,
    string ConfigTomlPath,
    HookUninstallOutcome NotifyOutcome,
    bool NotifyForeignRestored);
