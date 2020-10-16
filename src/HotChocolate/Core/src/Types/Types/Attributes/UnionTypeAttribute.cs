using System;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Interface,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class UnionTypeAttribute
        : UnionTypeDescriptorAttribute
    {
        public UnionTypeAttribute(string? name = null)
        {
            Name = name;
        }

        public string? Name { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IUnionTypeDescriptor descriptor,
            Type type)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                descriptor.Name(Name);
            }
        }
    }

}
