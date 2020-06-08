using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    internal static class DirectiveTypeDescriptorExtensions
    {
        private const string _fieldsArgument = "fields";

        public static IDirectiveTypeDescriptor FieldsArgument(
            this IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Argument(_fieldsArgument)
                .Type<NonNullType<FieldSetType>>();

            return descriptor;
        }
    }
}
