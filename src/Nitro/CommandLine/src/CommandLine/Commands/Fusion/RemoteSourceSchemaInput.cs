namespace ChilliCream.Nitro.CommandLine.Commands.Fusion;

internal sealed class RemoteSourceSchemaInput(
    Uri endpoint,
    string settingsFile)
{
    public Uri Endpoint { get; } = endpoint;

    public string SettingsFile { get; } = settingsFile;

    public string? Name { get; set; }
}
