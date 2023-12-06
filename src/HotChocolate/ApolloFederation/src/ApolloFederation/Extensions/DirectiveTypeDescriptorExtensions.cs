using static HotChocolate.ApolloFederation.Constants.WellKnownArgumentNames;
using FieldSetTypeV1 = HotChocolate.ApolloFederation.FieldSetType;
using FieldSetTypeV2 = HotChocolate.ApolloFederation.FieldSetType;

namespace HotChocolate.ApolloFederation;

internal static class DirectiveTypeDescriptorExtensions
{
    public static IDirectiveTypeDescriptor FieldsArgumentV1(
        this IDirectiveTypeDescriptor descriptor)
    {
        descriptor
            .Argument(Fields)
            .Type<NonNullType<FieldSetTypeV1>>();
        return descriptor;
    }

    public static IDirectiveTypeDescriptor FieldsArgumentV2(
        this IDirectiveTypeDescriptor descriptor)
    {
        descriptor
            .Argument(Fields)
            .Type<NonNullType<FieldSetTypeV2>>();
        return descriptor;
    }
}
