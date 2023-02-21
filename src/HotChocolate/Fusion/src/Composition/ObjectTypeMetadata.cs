using HotChocolate.Language;

namespace HotChocolate.Fusion.Composition;

public sealed class ObjectTypeMetadata
{
    public List<ObjectTypeFetcher> Fetchers { get; } = new();
}

public sealed class ObjectTypeFetcher
{
    public ObjectTypeFetcher(SelectionSetNode select)
    {
        Select = select;
    }

    public SelectionSetNode Select { get; }

    public Dictionary<string, FieldNode> Requirements { get; } = new();

    public Dictionary<string, FieldNode> Optionals { get; } = new();
}
