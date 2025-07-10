using HotChocolate.Features;

namespace HotChocolate.Types.Relay;

internal sealed class NodeTypeFeature : ISealable
{
    private NodeResolverInfo? _nodeResolver;

    public NodeResolverInfo? NodeResolver
    {
        get => _nodeResolver;
        set
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(
                    "The node resolver cannot be set after the feature is sealed.");
            }

            _nodeResolver = value;
        }
    }

    public bool IsReadOnly { get; private set; }

    public void Seal() => IsReadOnly = true;
}
