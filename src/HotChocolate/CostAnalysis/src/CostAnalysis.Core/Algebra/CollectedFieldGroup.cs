using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// A field selection and its parent type within a selection set.
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
    /// The field selection, or <see langword="null"/> when no selection is available.
    /// </param>
    /// <param name="member">
    /// The parent type and field definition for this selection.
    /// </param>
    /// <param name="inheritedSize">
    /// The list size inherited from the immediate parent, or <see langword="null"/> when none applies.
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
    /// Gets the parent type and field definition for this selection.
    /// </summary>
    public CollectedFieldGroupMember Member { get; }

    /// <summary>
    /// Gets the list size inherited from the immediate parent, or <see langword="null"/> when none applies.
    /// </summary>
    public double? InheritedSize { get; }

    /// <summary>
    /// Gets the field selection, or <see langword="null"/> when no selection is available.
    /// </summary>
    public FieldNode? Field { get; }
}
