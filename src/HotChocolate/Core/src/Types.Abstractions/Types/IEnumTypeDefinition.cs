using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// GraphQL Enum types, like Scalar types, also represent leaf values in a GraphQL type system.
/// However, Enum types describe the set of possible values.
/// </para>
/// <para>
/// Enums are not references for a numeric value, but are unique values in their own right.
/// They may serialize as a string: the name of the represented value.
/// </para>
/// <para>In this example, an Enum type called Direction is defined:</para>
/// <code>
/// enum Direction {
///   NORTH
///   EAST
///   SOUTH
///   WEST
/// }
/// </code>
/// </summary>
public interface IEnumTypeDefinition
    : IOutputTypeDefinition
    , IInputTypeDefinition
    , ISyntaxNodeProvider<EnumTypeDefinitionNode>
{
    /// <summary>
    /// Gets all possible values if this type.
    /// </summary>
    IReadOnlyEnumValueCollection Values { get; }
}
