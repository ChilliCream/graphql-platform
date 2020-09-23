using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation.Extensions
{
    public static class KeyInterfaceFieldDescriptorExtensions
    {
        public static IObjectTypeDescriptor Key(
            this IObjectTypeDescriptor descriptor, string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                TypeNames.Key,
                new ArgumentNode(
                    "fields",
                    new FieldSetType().ParseResult(fieldSet)
                )
            );
        }

        public static IInterfaceTypeDescriptor Key(
            this IInterfaceTypeDescriptor descriptor, string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                TypeNames.Key,
                new ArgumentNode(
                    "fields",
                    new FieldSetType().ParseResult(fieldSet)
                )
            );
        }
    }
}
