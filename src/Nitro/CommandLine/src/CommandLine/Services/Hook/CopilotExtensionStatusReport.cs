namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record CopilotExtensionStatusReport(
    string ExtensionPath, string ConfigPath, CopilotExtensionStatusOutcome Outcome);
