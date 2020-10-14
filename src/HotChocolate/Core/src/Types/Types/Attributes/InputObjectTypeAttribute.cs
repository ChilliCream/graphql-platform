using System;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class InputObjectTypeAttribute
        : InputObjectTypeDescriptorAttribute
    {
        public InputObjectTypeAttribute(string? name = null)
        {
            Name = name;
        }

        public string? Name { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IInputObjectTypeDescriptor descriptor,
            Type type)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                descriptor.Name(Name);
            }
        }
    }

}
