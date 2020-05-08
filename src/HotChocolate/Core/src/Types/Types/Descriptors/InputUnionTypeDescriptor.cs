using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class InputUnionTypeDescriptor
        : DescriptorBase<InputUnionTypeDefinition>
        , IInputUnionTypeDescriptor
    {
        protected InputUnionTypeDescriptor(IDescriptorContext context, Type clrType)
            : base(context)
        {
            if (clrType == null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            Definition.ClrType = clrType;
            Definition.Name = context.Naming.GetTypeName(clrType, TypeKind.InputUnion);
            Definition.Description = context.Naming.GetTypeDescription(
                clrType, TypeKind.InputUnion);
        }

        protected InputUnionTypeDescriptor(IDescriptorContext context)
            : base(context)
        {
            Definition.ClrType = typeof(object);
        }

        internal protected override InputUnionTypeDefinition Definition { get; } =
            new InputUnionTypeDefinition();

        protected override void OnCreateDefinition(InputUnionTypeDefinition definition)
        {
            if (Definition.ClrType is { })
            {
                Context.Inspector.ApplyAttributes(
                    Context,
                    this,
                    Definition.ClrType);
            }

            base.OnCreateDefinition(definition);
        }

        public IInputUnionTypeDescriptor SyntaxNode(
            InputUnionTypeDefinitionNode inputUnionTypeDefinitionNode)
        {
            Definition.SyntaxNode = inputUnionTypeDefinitionNode;
            return this;
        }

        public IInputUnionTypeDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public IInputUnionTypeDescriptor Description(string value)
        {
            Definition.Description = value;
            return this;
        }

        public IInputUnionTypeDescriptor Type<TInputObjectType>()
            where TInputObjectType : InputObjectType
        {
            Definition.Types.Add(new ClrTypeReference(
                typeof(TInputObjectType), TypeContext.Input));
            return this;
        }

        public IInputUnionTypeDescriptor Type<TInputObjectType>(
            TInputObjectType inputObjectType)
            where TInputObjectType : InputObjectType
        {
            if (inputObjectType == null)
            {
                throw new ArgumentNullException(nameof(inputObjectType));
            }
            Definition.Types.Add(new SchemaTypeReference(
                (ITypeSystemObject)inputObjectType));
            return this;
        }

        public IInputUnionTypeDescriptor Type(NamedTypeNode inputObjectType)
        {
            if (inputObjectType == null)
            {
                throw new ArgumentNullException(nameof(inputObjectType));
            }
            Definition.Types.Add(new SyntaxTypeReference(
                inputObjectType, TypeContext.Input));
            return this;
        }

        public IInputUnionTypeDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            Definition.AddDirective(directiveInstance);
            return this;
        }

        public IInputUnionTypeDescriptor Directive<T>()
            where T : class, new()
        {
            Definition.AddDirective(new T());
            return this;
        }

        public IInputUnionTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            Definition.AddDirective(name, arguments);
            return this;
        }

        public static InputUnionTypeDescriptor New(
            IDescriptorContext context,
            Type clrType) =>
            new InputUnionTypeDescriptor(context, clrType);

        public static InputUnionTypeDescriptor New(
            IDescriptorContext context) =>
            new InputUnionTypeDescriptor(context);

        public static InputUnionTypeDescriptor FromSchemaType(
            IDescriptorContext context,
            Type schemaType)
        {
            InputUnionTypeDescriptor descriptor = New(context, schemaType);
            descriptor.Definition.ClrType = typeof(object);
            return descriptor;
        }
    }
}
