using System;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class UnionTypeDescriptor
        : IUnionTypeDescriptor
        , IDescriptionFactory<UnionTypeDescription>
    {
        public UnionTypeDescriptor(Type clrType)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            UnionDescription.Name = clrType.GetGraphQLName();
            UnionDescription.Description = clrType.GetGraphQLDescription();
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

        public void Name(NameString name)
        {
            UnionDescription.Name = name.EnsureNotEmpty(nameof(name));
        }

        public void Description(string description)
        {
            UnionDescription.Description = description;
        }

        public void Type<TObjectType>()
        {
            UnionDescription.Types.Add(typeof(TObjectType).GetOutputType());
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

        IUnionTypeDescriptor IUnionTypeDescriptor.Name(NameString name)
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

        IUnionTypeDescriptor IUnionTypeDescriptor.Directive<T>(T directive)
        {
            UnionDescription.Directives.AddDirective(directive);
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.Directive<T>()
        {
            UnionDescription.Directives.AddDirective(new T());
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            UnionDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        IUnionTypeDescriptor IUnionTypeDescriptor.Directive(
            string name,
            params ArgumentNode[] arguments)
        {
            UnionDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
