namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Represents the configuration for a subgraph.
/// </summary>
public sealed class SubGraphConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubGraphConfiguration"/> class.
    /// </summary>
    /// <param name="name">The name of the subgraph.</param>
    /// <param name="schema">The schema associated with the subgraph.</param>
    /// <param name="extensions">The list of extensions to apply to the subgraph.</param>
    public SubGraphConfiguration(
        string name,
        string schema,
        params string[] extensions)
    {
        Name = name;
        Schema = schema;
        Extensions = extensions;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SubGraphConfiguration"/> class.
    /// </summary>
    /// <param name="name">The name of the subgraph.</param>
    /// <param name="schema">The schema associated with the subgraph.</param>
    /// <param name="extensions">The extension to apply to the subgraph.</param>
    public SubGraphConfiguration(
        string name,
        string schema,
        string extensions)
        : this(name, schema, new[] { extensions }) { }

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
}
