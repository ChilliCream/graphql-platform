using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    public abstract class DescriptorAttribute
        : Attribute
    {
        protected abstract void TryConfigure(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element);

        internal void TryConfigureInternal(
            IDescriptorContext context,
            IDescriptor descriptor,
            ICustomAttributeProvider element) =>
            TryConfigure(context, descriptor, element);
    }
}
