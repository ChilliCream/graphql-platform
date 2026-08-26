namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record CopilotExtensionUninstallReport(string ExtensionPath, string ConfigPath, bool Removed);
