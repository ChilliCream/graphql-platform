using System.Collections.Immutable;
using System.Text;
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
    private readonly byte[] _nameUtf8;
    private readonly byte[] _responseNameUtf8;

    public RepresentationShapeNode(
        string name,
        string responseName,
        ImmutableArray<RepresentationShapeNode> nodes,
        ImmutableArray<RepresentationShapeBranch> branches,
        int requirementIndex,
        ImmutableArray<string> lhsPath,
        bool isList,
        bool skipOnNull,
        ITypeNode? elementInputType,
        string? parentTypeCondition,
        string? typeCondition,
        bool requiresTypeName)
    {
        Name = name;
        _nameUtf8 = Encoding.UTF8.GetBytes(name);
        ResponseName = responseName;
        _responseNameUtf8 = string.Equals(name, responseName, StringComparison.Ordinal)
            ? _nameUtf8
            : Encoding.UTF8.GetBytes(responseName);
        Nodes = nodes;
        Branches = branches;
        RequirementIndex = requirementIndex;
        LhsPath = lhsPath;
        IsList = isList;
        SkipOnNull = skipOnNull;
        ElementInputType = elementInputType;
        ParentTypeCondition = parentTypeCondition;
        TypeCondition = typeCondition;
        RequiresTypeName = requiresTypeName;
    }

    /// <summary>
    /// Gets the source schema field name. This is the property name written
    /// into the representation object.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the UTF-8 encoded <see cref="Name"/>.
    /// </summary>
    public ReadOnlySpan<byte> NameUtf8 => _nameUtf8;

    /// <summary>
    /// Gets the response name under which the value is found in the local
    /// composite result. This differs from <see cref="Name"/> only when the
    /// field was aliased in the lookup selection.
    /// </summary>
    public string ResponseName { get; }

    /// <summary>
    /// Gets the UTF-8 encoded <see cref="ResponseName"/>.
    /// </summary>
    public ReadOnlySpan<byte> ResponseNameUtf8 => _responseNameUtf8;

    /// <summary>
    /// Gets the child nodes of this node. The array is default when this node
    /// is a leaf whose value is copied from a requirement value, and is a
    /// non-default array for composite and list nodes.
    /// </summary>
    public ImmutableArray<RepresentationShapeNode> Nodes { get; }

    /// <summary>
    /// Gets the type-conditioned branches of this composite node. Each branch
    /// contributes its nodes only when the value's runtime type satisfies
    /// the branch condition. The array is empty when this node has no
    /// conditioned branches.
    /// </summary>
    public ImmutableArray<RepresentationShapeBranch> Branches { get; }

    /// <summary>
    /// Gets the index of the requirement that supplies the value for this node.
    /// This is only set on leaf and list nodes.
    /// </summary>
    public int RequirementIndex { get; }

    /// <summary>
    /// Gets the property path that locates this node's value inside the
    /// requirement's input-shaped value. An empty path means the whole
    /// requirement value. Below a list node the path is relative to a single
    /// list element.
    /// </summary>
    public ImmutableArray<string> LhsPath { get; }

    /// <summary>
    /// Gets whether this node represents a list selection whose
    /// <see cref="Nodes"/> are resolved per list element.
    /// </summary>
    public bool IsList { get; }

    /// <summary>
    /// Gets whether the containing entity is unresolvable when this node's
    /// backing value is null. On a structural node a <c>false</c> value means
    /// a null backing value resolves every value below this node to null
    /// instead. On a leaf or list node this is set when the value feeds a
    /// non-null input position, which no null value can satisfy.
    /// </summary>
    public bool SkipOnNull { get; }

    /// <summary>
    /// Gets the declared input type syntax for elements of this node's backing
    /// list value, or <c>null</c> when the node is not list-valued or the
    /// element type is unknown. A null element cannot satisfy a non-null
    /// element position, which makes the containing entity unresolvable.
    /// This is set on leaf and list nodes.
    /// </summary>
    public ITypeNode? ElementInputType { get; }

    /// <summary>
    /// Gets the type condition that the containing element must satisfy for
    /// this node's value to be resolvable, or <c>null</c> when unconditional.
    /// </summary>
    public string? ParentTypeCondition { get; }

    /// <summary>
    /// Gets the type condition that this node's resolved value must satisfy,
    /// or <c>null</c> when unconditional.
    /// </summary>
    public string? TypeCondition { get; }

    /// <summary>
    /// Gets whether the emitter must write the runtime <c>__typename</c> into
    /// this composite node's object. This is set when the node's declared type
    /// is abstract and the node carries no type-conditioned branches, so the
    /// source schema cannot reconstruct the abstract value without its type name.
    /// </summary>
    public bool RequiresTypeName { get; }
}
