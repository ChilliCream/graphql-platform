using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

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
