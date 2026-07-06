using HotChocolate.Execution;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a data requirement that must be supplied as a variable to an
/// operation before it can be executed.
/// </summary>
/// <param name="Key">
/// The requirement key that names the variable carrying the requirement value.
/// </param>
/// <param name="Type">
/// The declared variable type in its syntax form. It is used to emit the variable
/// declaration in the subquery, to serialize the plan, and to check whether
/// projected values satisfy the declared nullability.
/// </param>
/// <param name="Path">
/// The selection path the requirement is anchored at.
/// </param>
/// <param name="Map">
/// The field selection map that projects the requirement value from the data.
/// </param>
public record OperationRequirement(
    string Key,
    ITypeNode Type,
    SelectionPath Path,
    IValueSelectionNode Map);
