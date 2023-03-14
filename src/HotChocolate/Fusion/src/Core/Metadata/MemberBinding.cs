namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// The type system member binding information.
/// </summary>
internal class MemberBinding
{
    /// <summary>
    /// Initializes a new instance of <see cref="MemberBinding"/>.
    /// </summary>
    /// <param name="subgraphName">
    /// The name of the subgraph to which the type system member is bound to.
    /// </param>
    /// <param name="name">
    /// The name which the type system member has in the <see cref="SubgraphName"/>.
    /// </param>
    public MemberBinding(string subgraphName, string name)
    {
        SubgraphName = subgraphName;
        Name = name;
    }

    /// <summary>
    /// Gets the name of the subgraph to which the type system member is bound to.
    /// </summary>
    public string SubgraphName { get; }

    /// <summary>
    /// Gets the name which the type system member has on a certain the subgraph
    /// represented by the <see cref="SubgraphName" />.
    /// </summary>
    public string Name { get; }
}
