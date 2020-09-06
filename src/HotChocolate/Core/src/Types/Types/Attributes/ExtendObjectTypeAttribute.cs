#nullable enable

using System;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class ExtendObjectTypeAttribute
        : ObjectTypeDescriptorAttribute
    {
        public ExtendObjectTypeAttribute(string? name = null)
        {
            Name = name;
        }

        public string? Name { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type type)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                descriptor.Name(Name);
            }
        }
    }
}
