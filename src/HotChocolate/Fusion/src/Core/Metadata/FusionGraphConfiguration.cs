using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// Represent the fusion graph configuration.
/// </summary>
internal sealed class FusionGraphConfiguration
{
    private readonly Dictionary<string, IType> _types;
    private readonly Dictionary<(string Schema, string Type), string> _typeNameLookup = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FusionGraphConfiguration"/> class.
    /// </summary>
    /// <param name="types">The list of types.</param>
    /// <param name="subgraphNames">The list of subgraph names.</param>
    /// <param name="httpClients">The list of HTTP clients.</param>
    /// <param name="webSocketClients">The list of WebSocket clients.</param>
    public FusionGraphConfiguration(
        IReadOnlyList<IType> types,
        IReadOnlyList<string> subgraphNames,
        IReadOnlyList<HttpClientConfiguration> httpClients,
        IReadOnlyList<WebSocketClientConfiguration> webSocketClients)
    {
        _types = types.ToDictionary(t => t.Name, StringComparer.Ordinal);
        SubgraphNames = subgraphNames;
        HttpClients = httpClients;
        WebSocketClients = webSocketClients;

        foreach (var type in types)
        {
            foreach (var binding in type.Bindings)
            {
                if (!binding.Name.EqualsOrdinal(type.Name))
                {
                    _typeNameLookup.Add((binding.SchemaName, binding.Name), type.Name);
                }
            }
        }
    }

    /// <summary>
    /// Gets the list of subgraph names.
    /// </summary>
    public IReadOnlyList<string> SubgraphNames { get; }

    /// <summary>
    /// Gets the list of HTTP clients.
    /// </summary>
    public IReadOnlyList<HttpClientConfiguration> HttpClients { get; }

    /// <summary>
    /// Gets the list of WebSocket clients.
    /// </summary>
    public IReadOnlyList<WebSocketClientConfiguration> WebSocketClients { get; }

    /// <summary>
    /// Gets the type of the specified name.
    /// </summary>
    /// <typeparam name="T">The type of the specified name.</typeparam>
    /// <param name="typeName">The name of the type.</param>
    /// <returns>The type of the specified name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type is not found.</exception>
    public T GetType<T>(string typeName) where T : IType
    {
        if (_types.TryGetValue(typeName, out var type) && type is T casted)
        {
            return casted;
        }

        throw new InvalidOperationException("Type not found.");
    }

    /// <summary>
    /// Gets the type of the specified name and schema name.
    /// </summary>
    /// <typeparam name="T">The type of the specified name.</typeparam>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="typeName">The name of the type.</param>
    /// <returns>The type of the specified name and schema name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the type is not found.</exception>
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

    /// <summary>
    /// Loads the configuration from the specified source text.
    /// </summary>
    /// <param name="sourceText">
    /// The source text that contains the configuration.
    /// </param>
    /// <returns>
    /// Returns a fusion graph configuration object.
    /// </returns>
    public static FusionGraphConfiguration Load(string sourceText)
        => new FusionGraphConfigurationReader().Read(sourceText);

    /// <summary>
    /// Loads the configuration from the specified source text.
    /// </summary>
    /// <param name="document">
    /// The document that contains the configuration.
    /// </param>
    /// <returns>
    /// Returns a fusion graph configuration object.
    /// </returns>
    public static FusionGraphConfiguration Load(DocumentNode document)
        => new FusionGraphConfigurationReader().Read(document);
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
