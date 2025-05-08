#nullable enable

namespace HotChocolate.Types.Relay;

internal sealed class NodeSchemaFeature
{
    public bool IsEnabled { get; set; } = true;

    public Dictionary<string, Type> NodeIdTypes { get; } = [];
}

internal sealed class NodeTypeFeature
{
    public NodeResolverInfo? NodeResolver { get; set; }
}
