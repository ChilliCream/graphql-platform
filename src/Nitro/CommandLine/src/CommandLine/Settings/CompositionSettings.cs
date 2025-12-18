namespace ChilliCream.Nitro.CommandLine.Settings;

internal sealed record CompositionSettings
{
    public MergerSettings? Merger { get; init; } = new();
}
