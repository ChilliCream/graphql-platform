using System;
using System.Collections.Immutable;
using HotChocolate.Internal;

namespace HotChocolate.Types
{
    internal class InterfaceTypeDescriptor
        : IInterfaceTypeDescriptor
    {
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public ResolveAbstractType ResolveAbstractType { get; protected set; }
        public ImmutableList<FieldDescriptor> Fields { get; protected set; }
            = ImmutableList<FieldDescriptor>.Empty;

        #region IObjectTypeDescriptor<T>

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            if (ValidationHelper.IsTypeNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL type name.",
                    nameof(name));
            }

            Name = name;
            return this;
        }
        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Description(string description)
        {
            Description = description;
            return this;
        }

        IFieldDescriptor IInterfaceTypeDescriptor.Field(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The field name cannot be null or empty.",
                    nameof(name));
            }

            if (ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL field name.",
                    nameof(name));
            }

            FieldDescriptor fieldDescriptor = new FieldDescriptor(Name, name);
            Fields = Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.ResolveAbstractType(
            ResolveAbstractType resolveAbstractType)
        {
            if (resolveAbstractType == null)
            {
                throw new ArgumentNullException(nameof(resolveAbstractType));
            }

            ResolveAbstractType = resolveAbstractType;
            return this;
        }

        #endregion
    }
}
