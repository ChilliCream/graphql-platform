namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the configuration for a subgraph.
/// </summary>
public sealed class SubgraphConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubgraphConfiguration"/> class.
    /// </summary>
    /// <param name="name">The name of the subgraph.</param>
    /// <param name="schema">The schema associated with the subgraph.</param>
    /// <param name="extensions">The list of extensions to apply to the subgraph.</param>
    /// <param name="clients">
    /// The list of clients that can be used to fetch data from this subgraph.
    /// </param>
    public SubgraphConfiguration(
        string name,
        string schema,
        IReadOnlyList<string> extensions,
        IReadOnlyList<IClientConfiguration> clients)
    {
        Name = name;
        Schema = schema;
        Clients = clients;
        Extensions = extensions;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubgraphConfiguration"/> class.
    /// </summary>
    /// <param name="name">The name of the subgraph.</param>
    /// <param name="schema">The schema associated with the subgraph.</param>
    /// <param name="extensions">The extension to apply to the subgraph.</param>
    public SubgraphConfiguration(
        string name,
        string schema,
        string extensions,
        IReadOnlyList<IClientConfiguration> clients)
        : this(name, schema, clients, new[] { extensions }) { }

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

    public IReadOnlyList<IClientConfiguration> Clients { get; }
}

public interface IClientConfiguration
{
    /// <summary>
    /// Gets the name of the client.
    /// </summary>
    string Name { get; }
}
