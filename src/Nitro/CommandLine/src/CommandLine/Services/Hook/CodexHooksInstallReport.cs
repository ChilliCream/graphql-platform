namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record CodexHooksInstallReport(
    string HooksJsonPath,
    IReadOnlyList<HookInstallEventResult> HooksEvents,
    string ConfigTomlPath,
    HookInstallOutcome NotifyOutcome,
    bool NotifyWrapsForeign);
