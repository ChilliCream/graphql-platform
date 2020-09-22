using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Extensions
{
    public static class KeyInterfaceFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor Key(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Directive(new KeyDirectiveType());
        }
    }
}
