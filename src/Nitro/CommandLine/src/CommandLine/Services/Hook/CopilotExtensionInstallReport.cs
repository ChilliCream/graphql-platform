namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record CopilotExtensionInstallReport(
    string ExtensionPath, string ConfigPath, CopilotExtensionInstallOutcome Outcome);
