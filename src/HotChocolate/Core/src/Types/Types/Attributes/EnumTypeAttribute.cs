using System;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class EnumTypeAttribute
        : EnumTypeDescriptorAttribute
    {
        public EnumTypeAttribute(string? name = null)
        {
            Name = name;
        }

        public string? Name { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IEnumTypeDescriptor descriptor,
            Type type)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                descriptor.Name(Name);
            }
        }
    }

}
