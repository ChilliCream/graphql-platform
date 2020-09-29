using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation.Extensions
{
    public static class RequiresDirectiveTypeDescriptorExtensions
    {
        public static IObjectFieldDescriptor Requires(
            this IObjectFieldDescriptor descriptor,
            string fieldSet)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(
                WellKnownTypeNames.Requires,
                new ArgumentNode(
                    "fields",
                    new FieldSetType().ParseResult(fieldSet)
                )
            );
        }
    }
}
