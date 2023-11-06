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

    public static SchemaException MutationConvDirective_In_Wrong_Location(
        DirectiveNode directiveNode)
        => new(SchemaErrorBuilder.New()
            .SetMessage(ThrowHelper_MutationConvDirective_In_Wrong_Location)
            .SetCode(ErrorCodes.Schema.MutationConvDirectiveWrongLocation)
            .AddSyntaxNode(directiveNode)
            .Build());

    public static SchemaException DirectiveArgument_Unexpected_Value(
        string argumentName,
        string typeName)
        => new(SchemaErrorBuilder.New()
            .SetMessage(ThrowHelper_DirectiveArgument_Unexpected_Value, argumentName, typeName)
            .SetCode(ErrorCodes.Schema.DirectiveArgumentUnexpectedValue)
            .Build());

    public static SchemaException UnknownDirectiveArgument(
        string argumentName)
        => new(SchemaErrorBuilder.New()
            .SetMessage(ThrowHelper_UnknownDirectiveArgument, argumentName)
            .SetCode(ErrorCodes.Schema.UnknownDirectiveArgument)
            .Build());
}
