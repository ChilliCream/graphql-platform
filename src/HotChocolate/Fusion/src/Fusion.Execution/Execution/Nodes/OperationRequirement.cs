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
/// <param name="Type">
/// The declared variable type in its syntax form. It is used to emit the variable
/// declaration in the subquery and to serialize the plan.
/// </param>
/// <param name="InputType">
/// The resolved input type of the requirement, or <c>null</c> when the named type
/// is not part of the schema and the nullability of input positions is therefore
/// unknown.
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
    IType? InputType,
    SelectionPath Path,
    IValueSelectionNode Map)
{
    /// <summary>
    /// Resolves the declared variable type against the schema. Returns <c>null</c>
    /// when the named type is not part of the schema, in which case the nullability
    /// of input positions is unknown.
    /// </summary>
    internal static IType? ResolveInputType(ITypeNode type, ISchemaDefinition schema)
    {
        if (schema.Types.TryGetType(type.NamedType().Name.Value, out var typeDefinition))
        {
            return type.RewriteToType(typeDefinition);
        }

        return null;
    }
}
