namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record CodexHooksStatusReport(
    string HooksJsonPath,
    IReadOnlyList<HookStatusEventResult> HooksEvents,
    string ConfigTomlPath,
    HookStatusOutcome NotifyOutcome);
