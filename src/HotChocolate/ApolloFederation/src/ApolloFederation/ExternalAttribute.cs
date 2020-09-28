using System;
using System.Reflection;
using HotChocolate.ApolloFederation.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ExternalAttribute: DescriptorAttribute
    {
        protected override void TryConfigure(IDescriptorContext context, IDescriptor descriptor, ICustomAttributeProvider element)
        {
            if (descriptor is IObjectFieldDescriptor ofd)
            {
                ofd.External();
            }
        }
    }
}
