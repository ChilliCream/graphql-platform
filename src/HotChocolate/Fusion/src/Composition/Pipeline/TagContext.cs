namespace HotChocolate.Fusion.Composition;

internal sealed class TagContext
{
    private static readonly HashSet<SchemaCoordinate> _empty = [];
    private readonly Dictionary<string, HashSet<SchemaCoordinate>> _taggedTypes =
        new(StringComparer.Ordinal);

    public bool HasTags { get; set; }

    public void RegisterTagCoordinate(string name, SchemaCoordinate coordinate)
    {
        if(_taggedTypes.TryGetValue(name, out var coordinates))
        {
            coordinates.Add(coordinate);
        }
        else
        {
            _taggedTypes.Add(name, [coordinate,]);
        }
    }

    public IReadOnlySet<SchemaCoordinate> GetTagCoordinates(string name)
        => _taggedTypes.GetValueOrDefault(name, _empty);
}
