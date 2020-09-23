using System;
using System.Reflection;
using HotChocolate.ApolloFederation.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.ApolloFederation
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class KeyAttribute : DescriptorAttribute
    {
        public KeyAttribute(string fieldSet = default)
        {
            FieldSet = fieldSet;
        }

        public NameString FieldSet { get; }

        protected override void TryConfigure(IDescriptorContext context, IDescriptor descriptor, ICustomAttributeProvider element)
        {
            if (descriptor is IInterfaceTypeDescriptor ifd)
            {
                ifd.Key(FieldSet);
            }

            if (descriptor is IObjectTypeDescriptor ad)
            {
                ad.Key(FieldSet);
            }
        }
    }
}
