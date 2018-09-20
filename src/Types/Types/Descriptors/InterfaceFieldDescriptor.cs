﻿using System;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InterfaceFieldDescriptor
        : IInterfaceFieldDescriptor
        , IDescriptionFactory<InterfaceFieldDescription>
    {
        protected InterfaceFieldDescriptor(
            InterfaceFieldDescription fieldDescription)
        {
            FieldDescription = fieldDescription
                ?? throw new ArgumentNullException(nameof(fieldDescription));
        }

        public InterfaceFieldDescriptor(string name)
        {
            FieldDescription = new InterfaceFieldDescription { Name = name };
        }

        public InterfaceFieldDescriptor()
        {
            FieldDescription = new InterfaceFieldDescription();
        }

        protected InterfaceFieldDescription FieldDescription { get; }

        public InterfaceFieldDescription CreateDescription()
        {
            return FieldDescription;
        }

        protected void SyntaxNode(FieldDefinitionNode syntaxNode)
        {
            FieldDescription.SyntaxNode = syntaxNode;
        }

        protected void Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL field name.",
                    nameof(name));
            }

            FieldDescription.Name = name;
        }

        protected void Description(string description)
        {
            FieldDescription.Description = description;
        }

        protected void Type<TOutputType>() where TOutputType : IOutputType
        {
            FieldDescription.TypeReference = FieldDescription
                .TypeReference.GetMoreSpecific(typeof(TOutputType));
        }

        protected void Type(ITypeNode type)
        {
            FieldDescription.TypeReference = FieldDescription
                .TypeReference.GetMoreSpecific(type);
        }

        protected void Argument(string name, Action<IArgumentDescriptor> argument)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The argument name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL argument name.",
                    nameof(name));
            }

            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }

            var descriptor = new ArgumentDescriptor(name);
            argument(descriptor);
            FieldDescription.Arguments.Add(descriptor.CreateDescription());
        }

        protected void DeprecationReason(string deprecationReason)
        {
            FieldDescription.DeprecationReason = deprecationReason;
        }

        #region IInterfaceFieldDescriptor

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.SyntaxNode(
            FieldDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Name(
            string name)
        {
            Name(name);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.DeprecationReason(
            string deprecationReason)
        {
            DeprecationReason(deprecationReason);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Type<TOutputType>()
        {
            Type<TOutputType>();
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Type(ITypeNode type)
        {
            Type(type);
            return this;
        }

        IInterfaceFieldDescriptor IInterfaceFieldDescriptor.Argument(
            string name, Action<IArgumentDescriptor> argument)
        {
            Argument(name, argument);
            return this;
        }

        #endregion
    }
}
