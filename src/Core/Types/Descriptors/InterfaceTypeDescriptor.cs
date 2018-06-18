using System;
using System.Collections.Immutable;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InterfaceTypeDescriptor
        : IInterfaceTypeDescriptor
    {
        public InterfaceTypeDescriptor(Type interfaceType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            // TODO : move name resolution to utilities
            Name = interfaceType.GetGraphQLName();
            if (Name == interfaceType.Name && Name.EndsWith("Type"))
            {
                Name = Name.Substring(0, Name.Length - 4);
            }
        }

        public InterfaceTypeDefinitionNode SyntaxNode { get; protected set; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public ResolveAbstractType ResolveAbstractType { get; protected set; }

        public ImmutableList<FieldDescriptor> Fields { get; protected set; }
            = ImmutableList<FieldDescriptor>.Empty;

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
