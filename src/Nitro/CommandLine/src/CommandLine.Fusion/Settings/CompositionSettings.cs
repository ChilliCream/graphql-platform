namespace ChilliCream.Nitro.CommandLine.Fusion.Settings;

internal sealed record CompositionSettings
{
    public MergerSettings? Merger { get; init; } = new();
}
