using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

internal sealed class ServiceConfiguration
{
    private readonly string[] _bindings;
    private readonly Dictionary<string, IType> _types;
    private readonly Dictionary<(string, string), string> _typeNameLookup = new();

    public ServiceConfiguration(IEnumerable<string> bindings, IEnumerable<IType> types)
    {
        _bindings = bindings.ToArray();
        _types = types.ToDictionary(t => t.Name, StringComparer.Ordinal);
    }

    // todo: Should be named SchemaNames or maybe SubGraphNames?
    public IReadOnlyList<string> Bindings => _bindings;

    public T GetType<T>(string typeName) where T : IType
    {
        if (_types.TryGetValue(typeName, out var type) && type is T casted)
        {
            return casted;
        }

        throw new InvalidOperationException("Type not found.");
    }

    public T GetType<T>(string schemaName, string typeName) where T : IType
    {
        if (!_typeNameLookup.TryGetValue((schemaName, typeName), out var temp))
        {
            temp = typeName;
        }

        if (_types.TryGetValue(temp, out var type) && type is T casted)
        {
            return casted;
        }

        throw new InvalidOperationException("Type not found.");
    }

    public T GetType<T>(TypeInfo typeInfo) where T : IType
    {
        throw new NotImplementedException();
    }

    public string GetTypeName(string schemaName, string typeName)
    {
        if (!_typeNameLookup.TryGetValue((schemaName, typeName), out var temp))
        {
            temp = typeName;
        }

        return temp;
    }

    public string GetTypeName(TypeInfo typeInfo)
    {
        throw new NotImplementedException();
    }

    public static ServiceConfiguration Load(string sourceText)
        => new ServiceConfigurationReader().Read(sourceText);

    public static ServiceConfiguration Load(DocumentNode document)
        => new ServiceConfigurationReader().Read(document);
}

public readonly struct TypeInfo
{
    public TypeInfo(string schemaName, string typeName)
    {
        SchemaName = schemaName;
        TypeName = typeName;
    }

    public string SchemaName { get; }

    public string TypeName { get; }
}
