using System;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
        Inherited = true,
        AllowMultiple = false)]
    public sealed class ObjectTypeAttribute
        : ObjectTypeDescriptorAttribute
    {
        public ObjectTypeAttribute(string? name = null)
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
