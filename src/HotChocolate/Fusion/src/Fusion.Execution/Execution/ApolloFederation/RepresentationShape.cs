using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.ApolloFederation;

/// <summary>
/// A node in the source-shaped representation tree for an Apollo Federation
/// <c>_entities</c> fetch. Each node corresponds to one property of the
/// representation object. A node is either a leaf that is supplied by a single
/// requirement value, a structural object that groups child nodes, or a list
/// whose child nodes are resolved per element of the backing list value.
/// <para>
/// Nodes are immutable and safe to share across concurrent executions.
/// </para>
/// </summary>
internal sealed class RepresentationShapeNode
{
    /// <summary>
    /// Gets the source schema field name. This is the property name written
    /// into the representation object.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the UTF-8 encoded <see cref="Name"/>.
    /// </summary>
    public required byte[] NameUtf8 { get; init; }

    /// <summary>
    /// Gets the response name under which the value is found in the local
    /// composite result. This differs from <see cref="Name"/> only when the
    /// field was aliased in the lookup selection.
    /// </summary>
    public required string ResponseName { get; init; }

    /// <summary>
    /// Gets the UTF-8 encoded <see cref="ResponseName"/>.
    /// </summary>
    public required byte[] ResponseNameUtf8 { get; init; }

    /// <summary>
    /// Gets the child nodes of this node, or <c>null</c> when this node is a
    /// leaf whose value is copied from a requirement value.
    /// </summary>
    public List<RepresentationShapeNode>? Children { get; set; }

    /// <summary>
    /// Gets the type-conditioned branches of this composite node. Each branch
    /// contributes its children only when the value's runtime type satisfies
    /// the branch condition, or null when this node has no conditioned branches.
    /// </summary>
    public List<RepresentationShapeBranch>? Branches { get; set; }

    /// <summary>
    /// Gets the index of the requirement that supplies the value for this node.
    /// This is only set on leaf and list nodes.
    /// </summary>
    public int RequirementIndex { get; set; } = -1;

    /// <summary>
    /// Gets the property path that locates this node's value inside the
    /// requirement's input-shaped value. An empty path means the whole
    /// requirement value. Below a list node the path is relative to a single
    /// list element.
    /// </summary>
    public string[] LhsPath { get; set; } = [];

    /// <summary>
    /// Gets whether this node represents a list selection whose
    /// <see cref="Children"/> are resolved per list element.
    /// </summary>
    public bool IsList { get; set; }

    /// <summary>
    /// Gets whether the containing entity is unresolvable when this node's
    /// backing value is null. On a structural node a <c>false</c> value means
    /// a null backing value resolves every value below this node to null
    /// instead. On a leaf or list node this is set when the value feeds a
    /// non-null input position, which no null value can satisfy.
    /// </summary>
    public bool SkipOnNull { get; set; }

    /// <summary>
    /// Gets the declared input type syntax for elements of this node's backing
    /// list value, or <c>null</c> when the node is not list-valued or the
    /// element type is unknown. A null element cannot satisfy a non-null
    /// element position, which makes the containing entity unresolvable.
    /// This is set on leaf and list nodes.
    /// </summary>
    public ITypeNode? ElementInputType { get; set; }

    /// <summary>
    /// Gets the type condition that the containing element must satisfy for
    /// this node's value to be resolvable, or <c>null</c> when unconditional.
    /// </summary>
    public string? ParentTypeCondition { get; set; }

    /// <summary>
    /// Gets the type condition that this node's resolved value must satisfy,
    /// or <c>null</c> when unconditional.
    /// </summary>
    public string? TypeCondition { get; set; }

    /// <summary>
    /// Gets whether the emitter must write the runtime <c>__typename</c> into
    /// this composite node's object. This is set when the node's declared type
    /// is abstract and the node carries no type-conditioned branches, so the
    /// source schema cannot reconstruct the abstract value without its type name.
    /// </summary>
    public bool RequiresTypeName { get; set; }
}

/// <summary>
/// A type-conditioned child set of a representation composite node.
/// </summary>
internal sealed class RepresentationShapeBranch
{
    /// <summary>
    /// Gets the branch condition type name.
    /// </summary>
    public required string TypeCondition { get; init; }

    /// <summary>
    /// Gets the child nodes emitted when this branch matches.
    /// </summary>
    public List<RepresentationShapeNode> Children { get; set; } = [];
}
