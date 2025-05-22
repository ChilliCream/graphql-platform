using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// <para>
/// A GraphQL Input Object defines a set of input fields; the input fields are either scalars,
/// enums, or other input objects. This allows arguments to accept arbitrarily complex structs.
/// </para>
/// <para>In this example, an Input Object called Point2D describes x and y inputs:</para>
///
/// <code>
/// input Point2D {
///   x: Float
///   y: Float
/// }
/// </code>
/// </summary>
public interface IInputObjectTypeDefinition
    : IInputTypeDefinition
    , ISyntaxNodeProvider<InputObjectTypeDefinitionNode>
{
    /// <summary>
    /// Gets the fields of this input object type.
    /// </summary>
    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Fields { get; }
}
