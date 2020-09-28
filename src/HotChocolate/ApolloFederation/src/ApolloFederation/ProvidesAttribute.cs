using System;
using System.Reflection;
using HotChocolate.ApolloFederation.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ProvidesAttribute: DescriptorAttribute
    {
        public string FieldSet { get; }

        public ProvidesAttribute(string fieldSet)
        {
            FieldSet = fieldSet;
        }

        protected override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IObjectFieldDescriptor ofd)
            {
                ofd.Provides(FieldSet);
            }
        }
    }
}
