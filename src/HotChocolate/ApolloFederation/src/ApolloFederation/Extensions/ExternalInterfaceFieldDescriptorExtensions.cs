using System;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation.Extensions
{
    public static class ExternalInterfaceFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor External(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new ExternalDirectiveType());
        }
    }
}
