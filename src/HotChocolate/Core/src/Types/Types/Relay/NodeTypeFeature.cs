using HotChocolate.Features;

namespace HotChocolate.Types.Relay;

internal sealed class NodeTypeFeature : ISealable
{
    public NodeResolverInfo? NodeResolver
    {
        get;
        set
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(
                    "The node resolver cannot be set after the feature is sealed.");
            }

            field = value;
        }
    }

    public bool IsReadOnly { get; private set; }

    public void Seal() => IsReadOnly = true;
}
