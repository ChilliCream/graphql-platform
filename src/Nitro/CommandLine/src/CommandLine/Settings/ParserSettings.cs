namespace ChilliCream.Nitro.CommandLine.Settings;

internal sealed record ParserSettings
{
    public bool? EnableSchemaValidation { get; init; }
}
