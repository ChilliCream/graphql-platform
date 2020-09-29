using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation.Extensions
{
    public static class ProvidesDirectiveTypeDescriptorExtensions
    {
        public static IObjectFieldDescriptor Provides(
            this IObjectFieldDescriptor descriptor,
            string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                WellKnownTypeNames.Provides,
                new ArgumentNode(
                    "fields",
                    new FieldSetType().ParseResult(fieldSet)
                )
            );
        }
    }
}
