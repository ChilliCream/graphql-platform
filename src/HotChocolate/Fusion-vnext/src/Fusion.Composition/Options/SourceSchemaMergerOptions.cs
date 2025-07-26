namespace HotChocolate.Fusion.Options;

public sealed class SourceSchemaMergerOptions
{
    public bool RemoveUnreferencedTypes { get; init; } = true;

    public bool AddFusionDefinitions { get; init; } = true;

    public bool EnableGlobalObjectIdentification { get; init; } = true;
}
