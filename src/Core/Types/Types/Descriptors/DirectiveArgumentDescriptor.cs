using System;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class DirectiveArgumentDescriptor
        : ArgumentDescriptorBase<DirectiveArgumentDefinition>
        , IDirectiveArgumentDescriptor
    {
        public DirectiveArgumentDescriptor(
            IDescriptorContext context,
            NameString argumentName)
            : base(context)
        {
            Definition.Name = argumentName;
            Definition.DefaultValue = NullValueNode.Default;
        }

        public DirectiveArgumentDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context)
        {
            Definition.Name = context.Naming.GetMemberName(
                property, MemberKind.DirectiveArgument);
            Definition.Description = context.Naming.GetMemberDescription(
                property, MemberKind.DirectiveArgument);
            Definition.Type = context.Inspector.GetInputReturnType(property);
            Definition.Property = property;
        }

        protected override void OnCreateDefinition(DirectiveArgumentDefinition definition)
        {
            base.OnCreateDefinition(definition);

            if (Definition.Property is { })
            {
                Context.Inspector.ApplyAttributes(this, Definition.Property);
            }
        }

        public new IDirectiveArgumentDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            base.SyntaxNode(inputValueDefinition);
            return this;
        }

        public IDirectiveArgumentDescriptor Name(
            NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }
        public new IDirectiveArgumentDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IDirectiveArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        public new IDirectiveArgumentDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            base.Type(inputType);
            return this;
        }

        public new IDirectiveArgumentDescriptor Type(
            ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IDirectiveArgumentDescriptor Type(
          Type type)
        {
            base.Type(type);
            return this;
        }

        public new IDirectiveArgumentDescriptor DefaultValue(
            IValueNode value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IDirectiveArgumentDescriptor DefaultValue(
            object value)
        {
            base.DefaultValue(value);
            return this;
        }

        public IDirectiveArgumentDescriptor Ignore()
        {
            Definition.Ignore = true;
            return this;
        }

        public static DirectiveArgumentDescriptor New(
            IDescriptorContext context,
            NameString argumentName) =>
            new DirectiveArgumentDescriptor(context, argumentName);

        public static DirectiveArgumentDescriptor New(
            IDescriptorContext context,
            PropertyInfo property) =>
            new DirectiveArgumentDescriptor(context, property);
    }
}
