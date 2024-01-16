using HotChocolate.ApolloFederation.Constants;

namespace HotChocolate.ApolloFederation;

internal static class CommonArgumentsDescriptorExtensions
{
    public static IDirectiveTypeDescriptor FieldsArgument(
        this IDirectiveTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        
        descriptor
            .Argument(WellKnownArgumentNames.Fields)
            .Type<NonNullType<FieldSetType>>();
        
        return descriptor;
    }
}
