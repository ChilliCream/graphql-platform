using HotChocolate.Execution;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a data requirement that must be supplied as a variable to an
/// operation before it can be executed.
/// </summary>
/// <param name="Key">
/// The requirement key that names the variable carrying the requirement value.
/// </param>
/// <param name="TypeNode">
/// The declared variable type in its syntax form. It is used to emit the variable
/// declaration in the subquery and to serialize the plan.
/// </param>
/// <param name="Type">
/// The input type of the requirement, including its list and non-null structure.
/// It defines the input positions that the projected requirement value must
/// satisfy.
/// </param>
/// <param name="Path">
/// The selection path the requirement is anchored at.
/// </param>
/// <param name="Map">
/// The field selection map that projects the requirement value from the data.
/// </param>
public record OperationRequirement(
    string Key,
    ITypeNode TypeNode,
    IInputType Type,
    SelectionPath Path,
    IValueSelectionNode Map);
