using System;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class UnionTypeDescriptor
        : DescriptorBase<UnionTypeDefinition>
        , IUnionTypeDescriptor
    {
        public UnionTypeDescriptor(IDescriptorContext context, Type clrType)
            : base(context)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Definition.ClrType = clrType;
            Definition.Name = context.Naming.GetTypeName(clrType);
            Definition.Description = context.Naming.GetTypeDescription(clrType);
        }

        public UnionTypeDescriptor(IDescriptorContext context, NameString name)
            : base(context)
        {
            Definition.ClrType = typeof(object);
            Definition.Name = name.EnsureNotEmpty(nameof(name));
        }

        protected override UnionTypeDefinition Definition { get; } =
            new UnionTypeDefinition();

        public IUnionTypeDescriptor SyntaxNode(
            UnionTypeDefinitionNode unionTypeDefinitionNode)
        {
            Definition.SyntaxNode = unionTypeDefinitionNode;
            return this;
        }

        public IUnionTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IUnionTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public IUnionTypeDescriptor Type<TObjectType>()
            where TObjectType : ObjectType
        {
            Definition.Types.Add(new ClrTypeReference(
                typeof(TObjectType), TypeContext.Output));
            return this;
        }

        public IUnionTypeDescriptor Type<TObjectType>(TObjectType objectType)
            where TObjectType : ObjectType
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }
            Definition.Types.Add(new SchemaTypeReference(objectType));
            return this;
        }

        public IUnionTypeDescriptor Type(NamedTypeNode objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }
            Definition.Types.Add(new SyntaxTypeReference(
                objectType, TypeContext.Output));
            return this;
        }

        public IUnionTypeDescriptor ResolveAbstractType(
            ResolveAbstractType resolveAbstractType)
        {
            Definition.ResolveAbstractType = resolveAbstractType
               ?? throw new ArgumentNullException(nameof(resolveAbstractType));
            return this;
        }

        public IUnionTypeDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance);
            return this;
        }

        public IUnionTypeDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T());
            return this;
        }

        public IUnionTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }
    }
}
