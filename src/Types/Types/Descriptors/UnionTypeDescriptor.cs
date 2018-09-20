using System;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class UnionTypeDescriptor
        : IUnionTypeDescriptor
        , IDescriptionFactory<UnionTypeDescription>
    {
        public UnionTypeDescriptor(Type unionType)
        {
            if (unionType == null)
            {
                throw new ArgumentNullException(nameof(unionType));
            }

            UnionDescription.Name = unionType.GetGraphQLName();
        }

        protected UnionTypeDescription UnionDescription { get; } =
            new UnionTypeDescription();

        public UnionTypeDescription CreateDescription()
        {
            return UnionDescription;
        }

        public void SyntaxNode(UnionTypeDefinitionNode syntaxNode)
        {
            UnionDescription.SyntaxNode = syntaxNode;
        }

        public void Name(string name)
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

            UnionDescription.Name = name;
        }

        public void Description(string description)
        {
            UnionDescription.Description = description;
        }

        public void Type<TObjectType>()
        {
            UnionDescription.Types.Add(new TypeReference(typeof(TObjectType)));
        }

        public void Type(NamedTypeNode objectType)
        {
            UnionDescription.Types.Add(new TypeReference(objectType));
        }

        public void ResolveAbstractType(
            ResolveAbstractType resolveAbstractType)
        {
            UnionDescription.ResolveAbstractType = resolveAbstractType
                ?? throw new ArgumentNullException(nameof(resolveAbstractType));
        }

        #region IUnionTypeDescriptor

        IUnionTypeDescriptor IUnionTypeDescriptor.SyntaxNode(
            UnionTypeDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.Name(string name)
        {
            Name(name);
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.Description(string description)
        {
            Description(description);
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.Type<TObjectType>()
        {
            Type<TObjectType>();
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.Type(NamedTypeNode objectType)
        {
            Type(objectType);
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.ResolveAbstractType(
            ResolveAbstractType resolveAbstractType)
        {
            ResolveAbstractType(resolveAbstractType);
            return this;
        }

        #endregion
    }
}
