using HotChocolate.ApolloFederation.Constants;

namespace HotChocolate.ApolloFederation;

internal static class DirectiveTypeDescriptorExtensions
{
    public static IDirectiveTypeDescriptor FieldsArgument(
        this IDirectiveTypeDescriptor descriptor)
    {
        descriptor
            .Argument(WellKnownArgumentNames.Fields)
            .Type<NonNullType<FieldSetType>>();
        return descriptor;
    }
}
