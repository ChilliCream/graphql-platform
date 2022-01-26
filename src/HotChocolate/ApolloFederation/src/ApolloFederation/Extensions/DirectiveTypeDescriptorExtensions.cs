using static HotChocolate.ApolloFederation.Constants.WellKnownArgumentNames;

namespace HotChocolate.ApolloFederation;

internal static class DirectiveTypeDescriptorExtensions
{
    public static IDirectiveTypeDescriptor FieldsArgument(
        this IDirectiveTypeDescriptor descriptor)
    {
        descriptor
            .Argument(Fields)
            .Type<NonNullType<FieldSetType>>();

        return descriptor;
    }
}
