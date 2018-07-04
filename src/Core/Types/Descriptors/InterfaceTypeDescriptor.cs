using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InterfaceTypeDescriptor
        : IInterfaceTypeDescriptor
        , IDescriptionFactory<InterfaceTypeDescription>
    {
        protected List<InterfaceFieldDescriptor> Fields { get; } =
            new List<InterfaceFieldDescriptor>();

        protected InterfaceTypeDescription ObjectDescription { get; } =
            new InterfaceTypeDescription();

        public InterfaceTypeDescription CreateDescription()
        {
            CompleteFields();
            return ObjectDescription;
        }

        protected virtual void CompleteFields()
        {
            foreach (InterfaceFieldDescriptor fieldDescriptor in Fields)
            {
                ObjectDescription.Fields.Add(
                    fieldDescriptor.CreateDescription());
            }
        }

        protected void SyntaxNode(InterfaceTypeDefinitionNode syntaxNode)
        {
            ObjectDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(string name)
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

            ObjectDescription.Name = name;
        }
        protected void Description(string description)
        {
            ObjectDescription.Description = description;
        }

        protected InterfaceFieldDescriptor Field(string name)
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

            InterfaceFieldDescriptor fieldDescriptor =
                new InterfaceFieldDescriptor(name);
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        protected void ResolveAbstractType(
            ResolveAbstractType resolveAbstractType)
        {
            if (resolveAbstractType == null)
            {
                throw new ArgumentNullException(nameof(resolveAbstractType));
            }

            ObjectDescription.ResolveAbstractType = resolveAbstractType;
        }

        #region IObjectTypeDescriptor<T>

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.SyntaxNode(
            InterfaceTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Name(string name)
        {
            Name(name);
            return this;
        }
        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Description(string description)
        {
            Description(description);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceTypeDescriptor.Field(string name)
        {
            return Field(name);
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.ResolveAbstractType(
            ResolveAbstractType resolveAbstractType)
        {
            ResolveAbstractType(resolveAbstractType);
            return this;
        }

        #endregion
    }
}
