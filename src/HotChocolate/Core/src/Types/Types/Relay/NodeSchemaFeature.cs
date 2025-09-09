namespace HotChocolate.Types.Relay;

internal sealed class NodeSchemaFeature
{
    public bool IsEnabled { get; set; } = true;

    public GlobalObjectIdentificationOptions Options { get; set; } = new();

    public Dictionary<string, Type> NodeIdTypes { get; } = [];
}
