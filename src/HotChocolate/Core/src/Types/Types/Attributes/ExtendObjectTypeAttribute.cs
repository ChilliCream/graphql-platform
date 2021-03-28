using System;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public sealed class ExtendObjectTypeAttribute : ObjectTypeDescriptorAttribute
    {
        private string? _name;

        public ExtendObjectTypeAttribute(string? name = null)
        {
            _name = name;
        }

        public ExtendObjectTypeAttribute(Type extendsType)
        {
            ExtendsType = extendsType;
        }

        /// <summary>
        /// Gets the GraphQL type name to which this extension is bound to.
        /// </summary>
        public string? Name
        {
            get => _name;
            [Obsolete("Use the new constructor.")] set => _name = value;
        }

        /// <summary>
        /// Gets the .NET type to which this extension is bound to.
        /// If this is a base type or an interface the extension will bind to all types
        /// inheriting or implementing the type.
        /// </summary>
        public Type? ExtendsType { get; }

        /// <summary>
        /// Gets a set of field names that will be removed from the extended type.
        /// </summary>
        public string[]? IgnoreFields { get; set; }

        /// <summary>
        /// Gets a set of property names that will be removed from the extended type.
        /// </summary>
        public string[]? IgnoreProperties { get; set; }

        /// <summary>
        /// Applies the type extension configuration.
        /// </summary>
        /// <param name="context">
        /// The descriptor context.
        /// </param>
        /// <param name="descriptor">
        /// The object type descriptor.
        /// </param>
        /// <param name="type">
        /// The type to which this instance is annotated to.
        /// </param>
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
}
