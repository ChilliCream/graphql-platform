using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// Represents a scalar type definition in the GraphQL schema.
/// </summary>
public interface IScalarTypeDefinition
    : IOutputTypeDefinition
    , IInputTypeDefinition
    , ISyntaxNodeProvider<ScalarTypeDefinitionNode>
{
    /// <summary>
    /// Gets the URI that specifies the behavior and constraints of this scalar type.
    /// </summary>
    Uri? SpecifiedBy { get; }

    /// <summary>
    /// Gets the possible types that this scalar can serialize to.
    /// </summary>
    ScalarSerializationType SerializationType { get; }

    /// <summary>
    /// Gets the ECMA-262 regex pattern that the serialized scalar value conforms
    /// to, if it's of the serialization type <see cref="ScalarSerializationType.String"/>.
    /// </summary>
    string? Pattern { get; }

    /// <summary>
    /// Checks if the value is an instance of this type.
    /// </summary>
    /// <param name="valueLiteral">The value to check.</param>
    /// <returns>
    /// <c>true</c> if the value is an instance of this type; otherwise, <c>false</c>.
    /// </returns>
    bool IsValueCompatible(IValueNode valueLiteral);
}
