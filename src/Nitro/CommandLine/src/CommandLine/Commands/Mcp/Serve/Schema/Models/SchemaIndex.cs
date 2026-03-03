namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class SchemaIndex
{
    private readonly Dictionary<string, SchemaIndexEntry> _members;
    private readonly Dictionary<string, List<string>> _typeToChildren;
    private readonly Dictionary<string, List<string>> _nameIndex;
    private readonly Dictionary<string, List<string>> _reverseEdges;

    public IReadOnlySet<string> RootTypes { get; }

    public SchemaIndex(
        Dictionary<string, SchemaIndexEntry> members,
        Dictionary<string, List<string>> typeToChildren,
        Dictionary<string, List<string>> nameIndex,
        Dictionary<string, List<string>> reverseEdges,
        IReadOnlySet<string> rootTypes)
    {
        _members = members;
        _typeToChildren = typeToChildren;
        _nameIndex = nameIndex;
        _reverseEdges = reverseEdges;
        RootTypes = rootTypes;
    }

    public SchemaIndexEntry? GetByCoordinate(string coordinate) => _members.GetValueOrDefault(coordinate);

    public IReadOnlyCollection<SchemaIndexEntry> GetAll() => _members.Values;

    public IReadOnlyList<string> GetChildCoordinates(string typeName)
        => _typeToChildren.GetValueOrDefault(typeName) ?? (IReadOnlyList<string>)Array.Empty<string>();

    public IReadOnlyList<string> GetCoordinatesByName(string nameLower)
        => _nameIndex.GetValueOrDefault(nameLower) ?? (IReadOnlyList<string>)Array.Empty<string>();

    public IReadOnlyList<string> GetIncomingEdges(string typeName)
        => _reverseEdges.GetValueOrDefault(typeName) ?? (IReadOnlyList<string>)Array.Empty<string>();

    public int Count => _members.Count;
}
