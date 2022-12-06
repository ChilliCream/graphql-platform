using System.Linq;
using static HotChocolate.Types.Properties.MutationResources;

namespace HotChocolate.Types;

internal static class ThrowHelper
{
    public static SchemaException MessageWasNotDefinedOnError(IType type, Type runtimeType) =>
        new(
            SchemaErrorBuilder.New()
                .SetMessage(
                    ThrowHelper_ErrorObjectType_MessageWasNotDefinedOnError,
                    type.GetType().FullName,
                    runtimeType.FullName)
                .Build());

    public static SchemaException TypeDoesNotExposeErrorFactory(Type errorType) =>
        new(
            SchemaErrorBuilder
                .New()
                .SetMessage(
                    ThrowHelper_ErrorFactoryCompiler_TypeDoesNotExposeErrorFactory,
                    errorType.FullName ?? errorType.Name)
                .Build());

    public static SchemaException CannotResolvePayloadType()
        => new(
            SchemaErrorBuilder.New()
                .SetMessage(ThrowHelper_CannotResolvePayloadType)
                .Build());

    public static SchemaException NonMutationFields(IEnumerable<MutationContextData> unprocessed)
        => new(
            SchemaErrorBuilder.New()
                .SetMessage(ThrowHelper_NonMutationFields)
                .SetExtension("fields", unprocessed.Select(t => t.Definition.Name).ToArray())
                .SetCode("")
                .Build());

    public static ISchemaError MutationPayloadMustBeObject(INamedType type)
        => SchemaErrorBuilder.New()
            .SetMessage(ThrowHelper_MutationPayloadMustBeObject, type.Name)
            .SetTypeSystemObject((ITypeSystemObject)type)
            .SetCode(ErrorCodes.Schema.MutationPayloadMustBeObject)
            .Build();
}
