using System;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Struct)]
    public sealed class InputObjectTypeAttribute
        : InputObjectTypeDescriptorAttribute
        , IInheritableAttribute
    {
        public InputObjectTypeAttribute(string? name = null)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the GraphQL type name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Defines if this attribute is inherited. The default is <c>false</c>.
        /// </summary>
        public bool Inherited { get; set; }

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
