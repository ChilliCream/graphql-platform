using System.Text.Json;

namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the configuration for a subgraph.
/// </summary>
public sealed class SubgraphConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubgraphConfiguration"/> class.
    /// </summary>
    /// <param name="name">
    /// The name of the subgraph.
    /// </param>
    /// <param name="schema">
    /// The schema associated with the subgraph.
    /// </param>
    /// <param name="extensions">
    /// The list of extensions to apply to the subgraph.
    /// </param>
    /// <param name="clients">
    /// The list of clients that can be used to fetch data from this subgraph.
    /// </param>
    /// <param name="configurationExtensions">
    /// The "extensions" field of the subgraph-config.json.
    /// </param>
    public SubgraphConfiguration(
        string name,
        string schema,
        IReadOnlyList<string> extensions,
        IReadOnlyList<IClientConfiguration> clients,
        JsonElement? configurationExtensions)
    {
        Name = name;
        Schema = schema;
        Extensions = extensions;
        Clients = clients;
        ConfigurationExtensions = configurationExtensions;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubgraphConfiguration"/> class.
    /// </summary>
    /// <param name="name">
    /// The name of the subgraph.
    /// </param>
    /// <param name="schema">
    /// The schema associated with the subgraph.
    /// </param>
    /// <param name="extensions">
    /// The extension to apply to the subgraph.
    /// </param>
    /// <param name="clients">
    /// The list of clients that can be used to fetch data from this subgraph.
    /// </param>
    /// <param name="configurationExtensions">
    /// The "extensions" field of the subgraph-config.json.
    /// </param>
    public SubgraphConfiguration(
        string name,
        string schema,
        string extensions,
        IReadOnlyList<IClientConfiguration> clients,
        JsonElement? configurationExtensions)
        : this(name, schema, new[] { extensions, }, clients, configurationExtensions) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubgraphConfiguration"/> class.
    /// </summary>
    /// <param name="name">
    /// The name of the subgraph.
    /// </param>
    /// <param name="schema">
    /// The schema associated with the subgraph.
    /// </param>
    /// <param name="extensions">
    /// The extension to apply to the subgraph.
    /// </param>
    /// <param name="client">
    /// The client that can be used to fetch data from this subgraph.
    /// </param>
    /// <param name="configurationExtensions">
    /// The "extensions" field of the subgraph-config.json.
    /// </param>
    public SubgraphConfiguration(
        string name,
        string schema,
        string extensions,
        IClientConfiguration client,
        JsonElement? configurationExtensions)
        : this(name, schema, new[] { extensions, }, new[] { client, }, configurationExtensions) { }

    /// <summary>
    /// Gets the name of the subgraph.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the schema associated with the subgraph.
    /// </summary>
    public string Schema { get; }

    /// <summary>
    /// Gets the list of extensions to apply to the subgraph.
    /// </summary>
    public IReadOnlyList<string> Extensions { get; }

    /// <summary>
    /// Gets the list of clients that can be used to fetch data from this subgraph.
    /// </summary>
    public IReadOnlyList<IClientConfiguration> Clients { get; }

    /// <summary>
    /// Gets the configuration extension.
    /// </summary>
    public JsonElement? ConfigurationExtensions { get; }
}
