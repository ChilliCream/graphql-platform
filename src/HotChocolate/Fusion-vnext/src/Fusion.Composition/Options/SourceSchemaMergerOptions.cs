namespace HotChocolate.Fusion.Options;

internal sealed class SourceSchemaMergerOptions
{
    public bool RemoveUnreferencedTypes { get; init; } = true;

    public bool AddFusionDefinitions { get; init; } = true;

    public bool EnableGlobalObjectIdentification { get; init; }
}
