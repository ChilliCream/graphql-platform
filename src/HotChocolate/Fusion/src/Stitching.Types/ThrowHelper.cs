using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Stitching.Types;

internal static class ThrowHelper
{
    public static GraphQLException ApplyExtensionsMiddleware_ArgumentCountMismatch(
        string typeName,
        FieldDefinitionNode definition,
        FieldDefinitionNode extension)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage($"The arguments for field {typeName}.{definition.Name} do not match with the type extension.")
                .SetExtension(nameof(typeName), typeName)
                .SetExtension("fieldName", definition.Name.Value)
                .SetExtension("expectedArgumentCount", definition.Arguments.Count)
                .SetExtension("argumentCount", extension.Arguments.Count)
                .Build());

    public static GraphQLException ApplyExtensionsMiddleware_UnexpectedArgumentName(
        string argumentName,
        string argumentExtName,
        int index,
        string typeName,
        string fieldName)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage($"Expected argument {argumentName} at position {index} on field {typeName}.{fieldName} but found {argumentExtName}.")
                .SetExtension(nameof(typeName), typeName)
                .SetExtension("fieldName", fieldName)
                .SetExtension("argumentIndex", index)
                .SetExtension("expectedArgument", argumentName)
                .SetExtension("argument", argumentExtName)
                .Build());

    public static GraphQLException ApplyExtensionsMiddleware_ArgumentTypeMismatch(
        InputValueDefinitionNode argument,
        InputValueDefinitionNode argumentExt,
        int index,
        string typeName,
        string fieldName)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage($"Expected {argument.Type} on argument {new SchemaCoordinate(typeName, fieldName, argument.Name.Value)} but found {argumentExt.Type}.")
                .SetExtension(nameof(typeName), typeName)
                .SetExtension("fieldName", fieldName)
                .SetExtension("argumentName", argument.Name.Value)
                .SetExtension("argumentIndex", index)
                .SetExtension("expectedArgumentType", argument.Type.ToString())
                .SetExtension("argumentType", argumentExt.Type.ToString())
                .Build());

    public static GraphQLException RenameDirectiveInvalidStructure(
        SchemaCoordinateNode schemaCoordinate)
        => new GraphQLException(
            ErrorBuilder.New()
                .SetMessage("The `@rename` directive must have exactly 1 argument called `to` and this argument must be a valid GraphQL name string.")
                .SetExtension("coordinate", schemaCoordinate.ToString())
                .Build());
}
