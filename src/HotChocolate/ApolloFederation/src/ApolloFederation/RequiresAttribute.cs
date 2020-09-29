using System;
using System.Reflection;
using HotChocolate.ApolloFederation.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RequiresAttribute : DescriptorAttribute
    {
        public RequiresAttribute(string fieldSet)
        {
            FieldSet = fieldSet;
        }

        public string FieldSet { get; }

        protected override void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element)
        {
            if (descriptor is IObjectFieldDescriptor ofd)
            {
                ofd.Requires(FieldSet);
            }
        }
    }
}
