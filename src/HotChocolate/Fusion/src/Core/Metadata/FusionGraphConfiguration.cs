using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// Represent the fusion graph configuration.
/// </summary>
internal sealed class FusionGraphConfiguration
{
    private readonly Dictionary<string, INamedTypeMetadata> _types;
    private readonly Dictionary<(string Schema, string Type), string> _typeNameLookup = new();
    private readonly Dictionary<(string Schema, string Type), string> _typeNameRevLookup = new();

    private readonly Dictionary<string, List<string>> _entitySubgraphMap =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="FusionGraphConfiguration"/> class.
    /// </summary>
    /// <param name="types">
    /// The list of types.
    /// </param>
    /// <param name="subgraphs">
    /// The list of entities.
    /// </param>
    /// <param name="httpClients">
    /// The list of HTTP clients.
    /// </param>
    /// <param name="webSocketClients">
    /// The list of WebSocket clients.
    /// </param>
    public FusionGraphConfiguration(
        IReadOnlyCollection<INamedTypeMetadata> types,
        IReadOnlyCollection<SubgraphInfo> subgraphs,
        IReadOnlyList<HttpClientConfiguration> httpClients,
        IReadOnlyList<WebSocketClientConfiguration> webSocketClients)
    {
        _types = types.ToDictionary(t => t.Name, StringComparer.Ordinal);

        foreach (var subgraph in subgraphs)
        {
            foreach (var entityName in subgraph.Entities)
            {
                if (!_entitySubgraphMap.TryGetValue(entityName, out var availableOnSubgraphs))
                {
                    availableOnSubgraphs = [];
                    _entitySubgraphMap.Add(entityName, availableOnSubgraphs);
                }
                availableOnSubgraphs.Add(subgraph.Name);
            }
        }

        SubgraphNames = subgraphs.OrderBy(t => t.Name).Select(t => t.Name).ToList();
        HttpClients = httpClients;
        WebSocketClients = webSocketClients;

        foreach (var type in types)
        {
            foreach (var binding in type.Bindings)
            {
                if (!binding.Name.EqualsOrdinal(type.Name))
                {
                    _typeNameLookup.Add((binding.SubgraphName, binding.Name), type.Name);
                    _typeNameRevLookup.Add((binding.SubgraphName, type.Name), binding.Name);
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
    public T GetType<T>(string typeName) where T : INamedTypeMetadata
    {
        if (_types.TryGetValue(typeName, out var type) && type is T casted)
        {
            return casted;
        }

        throw new InvalidOperationException("Type not found.");
    }

    public T GetType<T>(QualifiedTypeName qualifiedTypeName) where T : INamedTypeMetadata
    {
        var typeName = GetTypeName(qualifiedTypeName);

        if (_types.TryGetValue(typeName, out var type) && type is T casted)
        {
            return casted;
        }

        throw new InvalidOperationException("Type not found.");
    }

    public bool TryGetType<T>(string typeName, [NotNullWhen(true)] out T? type) where T : INamedTypeMetadata
    {
        if (_types.TryGetValue(typeName, out var value) && value is T casted)
        {
            type = casted;
            return true;
        }

        type = default!;
        return false;
    }

    public string GetTypeName(string subgraphName, string typeName)
    {
        if (!_typeNameLookup.TryGetValue((subgraphName, typeName), out var temp))
        {
            temp = typeName;
        }

        return temp;
    }

    public string GetTypeName(QualifiedTypeName qualifiedTypeName)
        => GetTypeName(qualifiedTypeName.SubgraphName, qualifiedTypeName.TypeName);

    /// <summary>
    /// Gets the subgraph type name of a fusion graph type.
    /// </summary>
    /// <param name="subgraphName">
    /// The name of the subgraph.
    /// </param>
    /// <param name="typeName">
    /// The name of the fusion graph type.
    /// </param>
    /// <returns></returns>
    public string GetSubgraphTypeName(string subgraphName, string typeName)
    {
        if (!_typeNameRevLookup.TryGetValue((subgraphName, typeName), out var subgraphTypeName))
        {
            subgraphTypeName = typeName;
        }

        return subgraphTypeName;
    }

    /// <summary>
    /// Gets the subgraphs that are able to resolve the specified entity.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <returns>
    /// Returns a list of subgraph names that are able to resolve the specified entity.
    /// </returns>
    public IReadOnlyList<string> GetAvailableSubgraphs(string entityName)
    {
        if (_entitySubgraphMap.TryGetValue(entityName, out var subgraphs))
        {
            return subgraphs;
        }

        return Array.Empty<string>();
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
