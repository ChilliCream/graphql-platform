using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One response-name group of collected fields at a selection-set boundary:
/// the parent-type/field-definition pair for every possible type the group
/// applies to, the list multiplier in effect at this boundary, and the
/// literal arguments and directives of the operation's field call, shared by
/// every occurrence merged into this group.
/// </summary>
[Experimental(CostExperiments.AnalysisAlgebra)]
public readonly ref struct CollectedFieldGroup
{
    /// <summary>
    /// Initializes a new instance of <see cref="CollectedFieldGroup"/>.
    /// </summary>
    /// <param name="responseName">
    /// The response name every field in <paramref name="members"/> shares.
    /// </param>
    /// <param name="members">
    /// The parent-type/field-definition pairs of this group, one per
    /// possible type the group applies to.
    /// </param>
    /// <param name="listMultiplier">
    /// The list multiplier in effect at this boundary.
    /// </param>
    /// <param name="arguments">
    /// The arguments supplied to this field call in the operation, as
    /// literal syntax.
    /// </param>
    /// <param name="directives">
    /// The directives applied to this field call in the operation, as
    /// literal syntax.
    /// </param>
    public CollectedFieldGroup(
        string responseName,
        ReadOnlySpan<CollectedFieldGroupMember> members,
        double listMultiplier,
        IReadOnlyList<ArgumentNode> arguments,
        IReadOnlyList<DirectiveNode> directives)
    {
        ResponseName = responseName;
        Members = members;
        ListMultiplier = listMultiplier;
        Arguments = arguments;
        Directives = directives;
    }

    /// <summary>
    /// Gets the response name every field in <see cref="Members"/> shares.
    /// </summary>
    public string ResponseName { get; }

    /// <summary>
    /// Gets the parent-type/field-definition pairs of this group, one per
    /// possible type the group applies to.
    /// </summary>
    public ReadOnlySpan<CollectedFieldGroupMember> Members { get; }

    /// <summary>
    /// Gets the list multiplier in effect at this boundary.
    /// </summary>
    public double ListMultiplier { get; }

    /// <summary>
    /// Gets the arguments supplied to this field call in the operation, as
    /// literal syntax; every occurrence merged into this group carries the
    /// same arguments.
    /// </summary>
    public IReadOnlyList<ArgumentNode> Arguments { get; }

    /// <summary>
    /// Gets the directives applied to this field call in the operation, as
    /// literal syntax; every occurrence merged into this group carries the
    /// same directives.
    /// </summary>
    public IReadOnlyList<DirectiveNode> Directives { get; }
}

/// <summary>
/// One possible type's contribution to a <see cref="CollectedFieldGroup"/>.
/// </summary>
/// <param name="ParentType">
/// The possible type the field is selected on.
/// </param>
/// <param name="Field">
/// The field definition <see cref="ParentType"/> resolves the selection to.
/// </param>
[Experimental(CostExperiments.AnalysisAlgebra)]
public readonly record struct CollectedFieldGroupMember(IComplexTypeDefinition ParentType, IOutputFieldDefinition Field);
