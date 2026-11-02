using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL scalar type definition in the GraphQL schema.
/// </summary>
public interface IScalarTypeDefinition
    : IOutputTypeDefinition
    , IInputTypeDefinition
    , ISyntaxNodeProvider<ScalarTypeDefinitionNode>
{
    /// <summary>
    /// Gets the URL that specifies the behavior and constraints of this scalar type.
    /// </summary>
    string? SpecifiedBy { get; }

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
    /// Checks if the value literal is compatible with this scalar type.
    /// </summary>
    /// <param name="valueLiteral">The value literal to check.</param>
    /// <returns>
    /// <c>true</c> if the value literal is compatible with this type; otherwise, <c>false</c>.
    /// </returns>
    bool IsValueCompatible(IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(valueLiteral);

        // A scalar whose serialization type is unknown cannot reject any literal.
        if (SerializationType is ScalarSerializationType.Undefined)
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.String) == ScalarSerializationType.String
            && valueLiteral is { Kind: SyntaxKind.StringValue })
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Int) == ScalarSerializationType.Int
            && valueLiteral is { Kind: SyntaxKind.IntValue })
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Float) == ScalarSerializationType.Float
            && valueLiteral is { Kind: SyntaxKind.FloatValue or SyntaxKind.IntValue })
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Boolean) == ScalarSerializationType.Boolean
            && valueLiteral is { Kind: SyntaxKind.BooleanValue })
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.List) == ScalarSerializationType.List
            && valueLiteral is { Kind: SyntaxKind.ListValue })
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Object) == ScalarSerializationType.Object
            && valueLiteral is { Kind: SyntaxKind.ObjectValue })
        {
            return true;
        }

        return false;
    }
}
