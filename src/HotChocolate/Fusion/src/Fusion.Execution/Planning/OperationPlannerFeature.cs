namespace HotChocolate.Fusion.Planning;

public sealed class OperationPlannerFeature
{
    public OperationPlannerFeature(Version version)
    {
        ArgumentNullException.ThrowIfNull(version);

        Version = version;
    }

    public Version Version { get; }

    public string? ConfigurationId { get; init; }
}
