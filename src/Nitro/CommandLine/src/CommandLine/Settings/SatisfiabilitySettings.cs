namespace ChilliCream.Nitro.CommandLine.Settings;

internal sealed record SatisfiabilitySettings
{
    public Dictionary<string, List<string>>? IgnoredNonAccessibleFields { get; init; }
}
