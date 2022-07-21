namespace HotChocolate.Fusion.Types;

public sealed class Schema
{
    private readonly string[] _bindings;
    private readonly Dictionary<string, IType> _types;

    public Schema(IEnumerable<string> bindings, IEnumerable<IType> types)
    {
        _bindings = bindings.ToArray();
        _types = types.ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<string> Bindings => _bindings;

    public T GetType<T>(string typeName) where T : IType
    {
        if (_types.TryGetValue(typeName, out var type) && type is T casted)
        {
            return casted;
        }

        throw new InvalidOperationException("Type not found.");
    }

    public static Schema Load(string body) => throw new NotImplementedException();
}
