#nullable enable

using System;
using System.Reflection;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public sealed class ExtendObjectTypeAttribute
        : ObjectTypeDescriptorAttribute
    {
        public ExtendObjectTypeAttribute(string? name = null)
        {
            Name = name;
        }

        public ExtendObjectTypeAttribute(Type extendsType)
        {
            ExtendsType = extendsType;
        }

        public string? Name { get; set; }

        public Type? ExtendsType { get; }

        public string[] IgnoreFields { get; set; }

        public string[] IgnoreProperties { get; set; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type type)
        {
            if (ExtendsType is not null)
            {
                descriptor.ExtendsType(ExtendsType);
                descriptor.Name("_" + Guid.NewGuid().ToString("N"));
            }

            if (!string.IsNullOrEmpty(Name))
            {
                descriptor.Name(Name);
            }
        }
    }

    public sealed class BindPropertyAttribute : ObjectFieldDescriptorAttribute
    {
        public BindPropertyAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                descriptor.Extend().OnBeforeCreate(
                    d => d.BindTo = new ObjectFieldBinding(Name, ObjectFieldBindingType.Property));
            }
        }
    }

    public sealed class BindFieldAttribute : ObjectFieldDescriptorAttribute
    {
        public BindFieldAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                descriptor.Extend().OnBeforeCreate(
                    d => d.BindTo = new ObjectFieldBinding(Name, ObjectFieldBindingType.Field));
            }
        }
    }
}
