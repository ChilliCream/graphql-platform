using System;
using System.Collections.Immutable;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class UnionTypeDescriptor
        : IUnionTypeDescriptor
    {
        public UnionTypeDescriptor(Type unionType)
        {
            if (unionType == null)
            {
                throw new ArgumentNullException(nameof(unionType));
            }

            // TODO : move name resolution to utilities
            Name = unionType.GetGraphQLName();
            if (Name == unionType.Name && Name.EndsWith("Type"))
            {
                Name = Name.Substring(0, Name.Length - 4);
            }
        }

        public UnionTypeDefinitionNode SyntaxNode { get; protected set; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public ImmutableList<TypeReference> Types { get; protected set; }
            = ImmutableList<TypeReference>.Empty;

        public ResolveAbstractType ResolveAbstractType { get; protected set; }

        #region IUnionTypeDescriptor

        IUnionTypeDescriptor IUnionTypeDescriptor.SyntaxNode(
            UnionTypeDefinitionNode syntaxNode)
        {
            SyntaxNode = syntaxNode;
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.Name(string name)
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

        IUnionTypeDescriptor IUnionTypeDescriptor.Description(string description)
        {
            Description = description;
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.Type<TObjectType>()
        {
            Types = Types.Add(new TypeReference(typeof(TObjectType)));
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.Type(NamedTypeNode objectType)
        {
            Types = Types.Add(new TypeReference(objectType));
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.ResolveAbstractType(
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
