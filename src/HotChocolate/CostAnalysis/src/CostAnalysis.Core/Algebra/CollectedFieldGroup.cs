using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One response-name group of collected fields at a selection-set boundary:
/// the parent-type/field-definition pair for every possible type the group
/// applies to, and the list multiplier in effect at this boundary.
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
    public CollectedFieldGroup(
        string responseName,
        ReadOnlySpan<CollectedFieldGroupMember> members,
        double listMultiplier)
    {
        ResponseName = responseName;
        Members = members;
        ListMultiplier = listMultiplier;
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
