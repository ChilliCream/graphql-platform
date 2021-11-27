using System;
using HotChocolate.Properties;

namespace HotChocolate.Types.Errors;

public static class ThrowHelper
{
    public static Exception TypeInspectorCouldNotBeLoaded(IType type) =>
        new SchemaException(SchemaErrorBuilder.New()
            .SetMessage(
                TypeResources.ThrowHelper_ErrorObjectType_TypeInspectorCouldNotBeLoaded,
                type.GetType().FullName)
            .Build());

    public static Exception MessageWasNotDefinedOnError(IType type, Type runtimeType) =>
        new SchemaException(SchemaErrorBuilder.New()
            .SetMessage(
                TypeResources.ThrowHelper_ErrorObjectType_MessageWasNotDefinedOnError,
                type.GetType().FullName,
                runtimeType.FullName)
            .Build());

    public static Exception TypeDoesNotExposeErrorFactory(Type errorType) =>
        new SchemaException(SchemaErrorBuilder
            .New()
            .SetMessage(
                TypeResources.ThrowHelper_ErrorFactoryCompiler_TypeDoesNotExposeErrorFactory,
                errorType.FullName ?? errorType.Name)
            .Build());
}
