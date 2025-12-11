namespace ChilliCream.Nitro.CommandLine.Fusion.Settings;

internal sealed record SatisfiabilitySettings
{
    public Dictionary<string, List<string>>? IgnoredNonAccessibleFields { get; init; }
}
