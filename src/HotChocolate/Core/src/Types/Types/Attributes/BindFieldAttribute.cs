using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Binds a member of a type extension to a field of the actual type.
    /// </summary>
    public sealed class BindFieldAttribute : ObjectFieldDescriptorAttribute
    {
        /// <summary>
        /// Binds a member of a type extension to a field of the actual type.
        /// </summary>
        public BindFieldAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The GraphQL field name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Defines if the members shall be merged or if this member with all its settings
        /// will replace the original one.
        /// </summary>
        public bool Replace { get; set; } = true;

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                descriptor.Extend().OnBeforeCreate(
                    d => d.BindTo = new ObjectFieldBinding(
                        Name,
                        ObjectFieldBindingType.Field,
                        Replace));
            }
        }
    }
}
