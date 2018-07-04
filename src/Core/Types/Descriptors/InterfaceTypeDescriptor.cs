using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InterfaceTypeDescription
    {
        public InterfaceTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ResolveAbstractType ResolveAbstractType { get; set; }

        public List<InterfaceFieldDescription> Fields { get; set; } =
            new List<InterfaceFieldDescription>();
    }

    internal class InterfaceTypeDescriptor
        : IInterfaceTypeDescriptor
    {
        protected List<InputFieldDescriptor> Fields { get; } =
            new List<InputFieldDescriptor>();

        protected InterfaceTypeDescription ObjectDescription { get; }
            = new InterfaceTypeDescription();

        public InputObjectTypeDescription CreateObjectDescription()
        {
            CompleteFields();
            return ObjectDescription;
        }

        protected virtual void CompleteFields()
        {
            foreach (InputFieldDescriptor fieldDescriptor in Fields)
            {
                ObjectDescription.Fields.Add(
                    fieldDescriptor.CreateInputDescription());
            }
        }

        #region IObjectTypeDescriptor<T>

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.SyntaxNode(
            InterfaceTypeDefinitionNode syntaxNode)
        {
            SyntaxNode = syntaxNode;
            return this;
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsTypeNameValid(name))
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

        IInterfaceFieldDescriptor IInterfaceTypeDescriptor.Field(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The field name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL field name.",
                    nameof(name));
            }

            ObjectFieldDescriptor fieldDescriptor = new ObjectFieldDescriptor(Name, name);
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
