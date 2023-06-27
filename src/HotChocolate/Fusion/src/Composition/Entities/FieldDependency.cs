namespace HotChocolate.Fusion.Composition;

/// <summary>
/// Describes the dependency a field has to another subgraph.
/// </summary>
internal sealed class FieldDependency
{
    public FieldDependency(int id, string subgraphName)
    {
        Id = id;
        SubgraphName = subgraphName;
    }

    /// <summary>
    /// Gets the internal ID of this dependency,
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the name of the subgraph in which it depends on other subgraph data.
    /// There might be multiple resolver overloads that have different dependencies.
    /// </summary>
    public string SubgraphName { get; }

    /// <summary>
    /// The arguments that represent dependencies.
    /// </summary>
    public Dictionary<string, MemberReference> Arguments { get; } = new();
}
