using System.Collections.Immutable;

namespace HotChocolate.Fusion.Execution.ApolloFederation;

/// <summary>
/// A type-conditioned child set of a representation composite node.
/// </summary>
internal sealed class RepresentationShapeBranch
{
    public RepresentationShapeBranch(
        string typeCondition,
        ImmutableArray<RepresentationShapeNode> nodes)
    {
        TypeCondition = typeCondition;
        Nodes = nodes;
    }

    /// <summary>
    /// Gets the branch condition type name.
    /// </summary>
    public string TypeCondition { get; }

    /// <summary>
    /// Gets the child nodes emitted when this branch matches.
    /// </summary>
    public ImmutableArray<RepresentationShapeNode> Nodes { get; }
}
