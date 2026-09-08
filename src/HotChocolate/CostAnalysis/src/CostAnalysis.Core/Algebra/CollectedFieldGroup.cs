using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One collected field occurrence and possible parent-type pair at a
/// selection-set boundary.
/// </summary>
[Experimental(CostExperiments.AnalysisAlgebra)]
public readonly ref struct CollectedFieldGroup
{
    /// <summary>
    /// Initializes a new instance of <see cref="CollectedFieldGroup"/>.
    /// </summary>
    /// <param name="responseName">
    /// The response name of <paramref name="field"/>.
    /// </param>
    /// <param name="field">
    /// The field occurrence.
    /// </param>
    /// <param name="member">
    /// The parent-type/field-definition pair for this occurrence.
    /// </param>
    /// <param name="inheritedSize">
    /// The size inherited from the immediate parent, if applicable.
    /// </param>
    public CollectedFieldGroup(
        string responseName,
        FieldNode? field,
        CollectedFieldGroupMember member,
        double? inheritedSize)
    {
        ResponseName = responseName;
        Field = field;
        Member = member;
        InheritedSize = inheritedSize;
    }

    /// <summary>
    /// Gets the response name of <see cref="Field"/>.
    /// </summary>
    public string ResponseName { get; }

    /// <summary>
    /// Gets the parent-type/field-definition pair for this occurrence.
    /// </summary>
    public CollectedFieldGroupMember Member { get; }

    /// <summary>
    /// Gets the size inherited from the immediate parent, if applicable.
    /// </summary>
    public double? InheritedSize { get; }

    /// <summary>
    /// Gets the field occurrence.
    /// </summary>
    public FieldNode? Field { get; }
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
