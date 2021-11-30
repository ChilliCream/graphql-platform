using System;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Properties;

namespace HotChocolate.Types;

internal static class ThrowHelper
{
    public static Exception MessageWasNotDefinedOnError(IType type, Type runtimeType) =>
        new SchemaException(SchemaErrorBuilder.New()
            .SetMessage(
                MutationResources.ThrowHelper_ErrorObjectType_MessageWasNotDefinedOnError,
                type.GetType().FullName,
                runtimeType.FullName)
            .Build());

    public static Exception TypeDoesNotExposeErrorFactory(Type errorType) =>
        new SchemaException(SchemaErrorBuilder
            .New()
            .SetMessage(
                MutationResources.ThrowHelper_ErrorFactoryCompiler_TypeDoesNotExposeErrorFactory,
                errorType.FullName ?? errorType.Name)
            .Build());

    public static Exception ArgumentTypeNameMissMatch(
        ObjectTypeDefinition objectTypeDefinition,
        string generatedArgumentName,
        ObjectFieldDefinition fieldDefinition,
        string currentTypeName,
        string collidingTypeName) =>
        new SchemaException(SchemaErrorBuilder.New()
            .SetMessage(
                MutationResources.ThrowHelper_InputMiddleware_ArgumentTypeNameMissMatch,
                generatedArgumentName,
                $"{objectTypeDefinition.Name}.{fieldDefinition.Name}",
                currentTypeName,
                collidingTypeName,
                objectTypeDefinition.Name)
            .Build());
}
