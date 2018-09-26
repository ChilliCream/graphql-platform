using System;
using System.Collections.Generic;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InterfaceTypeDescriptor
        : IInterfaceTypeDescriptor
        , IDescriptionFactory<InterfaceTypeDescription>
    {
        protected List<InterfaceFieldDescriptor> Fields { get; } =
            new List<InterfaceFieldDescriptor>();

        protected InterfaceTypeDescription InterfaceDescription { get; } =
            new InterfaceTypeDescription();

        public InterfaceTypeDescription CreateDescription()
        {
            CompleteFields();
            return InterfaceDescription;
        }

        protected virtual void CompleteFields()
        {
            foreach (InterfaceFieldDescriptor fieldDescriptor in Fields)
            {
                InterfaceDescription.Fields.Add(
                    fieldDescriptor.CreateDescription());
            }
        }

        protected void SyntaxNode(InterfaceTypeDefinitionNode syntaxNode)
        {
            InterfaceDescription.SyntaxNode = syntaxNode;
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

            InterfaceDescription.Name = name;
        }
        protected void Description(string description)
        {
            InterfaceDescription.Description = description;
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

            var fieldDescriptor = new InterfaceFieldDescriptor(name);
            Fields.Add(fieldDescriptor);
            return fieldDescriptor;
        }

        protected void ResolveAbstractType(
            ResolveAbstractType resolveAbstractType)
        {
            InterfaceDescription.ResolveAbstractType = resolveAbstractType
                ?? throw new ArgumentNullException(nameof(resolveAbstractType));
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

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Directive<T>(
            T directive)
        {
            InterfaceDescription.Directives.AddDirective(directive);
            return this;
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Directive<T>()
        {
            InterfaceDescription.Directives.AddDirective(new T());
            return this;
        }

        IInterfaceTypeDescriptor IInterfaceTypeDescriptor.Directive(
            string name,
            params ArgumentNode[] arguments)
        {
            InterfaceDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
