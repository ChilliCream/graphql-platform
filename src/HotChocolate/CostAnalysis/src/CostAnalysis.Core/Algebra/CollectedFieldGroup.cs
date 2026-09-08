using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One response-name group of collected fields at a selection-set boundary:
/// the parent-type/field-definition pair for every possible type the group
/// applies to, the sizes inherited from a parent's <c>@listSize(sizedFields:)</c>,
/// and every field occurrence merged into this group.
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
    /// <param name="inheritedSizes">
    /// The sizes inherited from every possible parent whose
    /// <c>@listSize(sizedFields:)</c> names this field, empty when none do.
    /// </param>
    /// <param name="fields">
    /// The field occurrences merged into this response-name group.
    /// </param>
    public CollectedFieldGroup(
        string responseName,
        ReadOnlySpan<CollectedFieldGroupMember> members,
        ReadOnlySpan<double> inheritedSizes,
        IReadOnlyList<FieldNode> fields)
    {
        ResponseName = responseName;
        Members = members;
        InheritedSizes = inheritedSizes;
        Fields = fields;
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
    /// Gets the sizes inherited from every possible parent whose
    /// <c>@listSize(sizedFields:)</c> names this field, empty when none do.
    /// </summary>
    public ReadOnlySpan<double> InheritedSizes { get; }

    /// <summary>
    /// Gets every field occurrence merged into this response-name group.
    /// </summary>
    public IReadOnlyList<FieldNode> Fields { get; }
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
