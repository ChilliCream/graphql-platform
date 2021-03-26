#nullable enable

using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public sealed class ExtendObjectTypeAttribute : ObjectTypeDescriptorAttribute
    {
        public ExtendObjectTypeAttribute(string? name = null)
        {
            Name = name;
        }

        public ExtendObjectTypeAttribute(Type extendsType)
        {
            ExtendsType = extendsType;
        }

        public string? Name { get; [Obsolete("Use the new constructor.")] set; }

        public Type? ExtendsType { get; }

        public string[]? IgnoreFields { get; set; }

        public string[]? IgnoreProperties { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type type)
        {
            if (ExtendsType is not null)
            {
                descriptor.ExtendsType(ExtendsType);
            }

            if (!string.IsNullOrEmpty(Name))
            {
                descriptor.Name(Name);
            }

            if (IgnoreFields is not null)
            {
                descriptor.Extend().OnBeforeCreate(d =>
                {
                    foreach (string fieldName in IgnoreFields)
                    {
                        d.FieldIgnores.Add(new ObjectFieldBinding(
                            fieldName,
                            ObjectFieldBindingType.Field));
                    }
                });
            }

            if (IgnoreProperties is not null)
            {
                descriptor.Extend().OnBeforeCreate(d =>
                {
                    foreach (string fieldName in IgnoreProperties)
                    {
                        d.FieldIgnores.Add(new ObjectFieldBinding(
                            fieldName,
                            ObjectFieldBindingType.Property));
                    }
                });
            }
        }
    }

    /// <summary>
    /// Binds a member of a type extension to a member of the actual type.
    /// </summary>
    public sealed class BindMemberAttribute : ObjectFieldDescriptorAttribute
    {
        public BindMemberAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The member name.
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
                        ObjectFieldBindingType.Property,
                        Replace));
            }
        }
    }

    /// <summary>
    /// Binds a member of a type extension to a field of the actual type.
    /// </summary>
    public sealed class BindFieldAttribute : ObjectFieldDescriptorAttribute
    {
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
