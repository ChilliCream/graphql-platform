namespace HotChocolate.Fusion.Composition;

internal sealed class TagContext
{
    private static readonly HashSet<SchemaCoordinate> _empty = new();
    private readonly Dictionary<string, HashSet<SchemaCoordinate>> _taggedTypes =
        new(StringComparer.Ordinal);

    public bool HasTags { get; set; } = false;
    
    public void RegisterTagCoordinate(string name, SchemaCoordinate coordinate)
    {
        if(_taggedTypes.TryGetValue(name, out var coordinates))
        {
            coordinates.Add(coordinate);
        }
        else
        {
            _taggedTypes.Add(name, new HashSet<SchemaCoordinate> { coordinate });
        }
    }
    
    public IReadOnlySet<SchemaCoordinate> GetTagCoordinates(string name)
        => _taggedTypes.TryGetValue(name, out var coordinates) ? coordinates : _empty;
}