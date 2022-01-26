using HotChocolate.Types;
using static HotChocolate.ApolloFederation.Constants.WellKnownArgumentNames;

namespace HotChocolate.ApolloFederation.Extensions;

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
