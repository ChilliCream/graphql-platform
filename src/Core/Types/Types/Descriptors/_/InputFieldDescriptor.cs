using System;
using System.Reflection;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class InputFieldDescriptor
        : ArgumentDescriptorBase<InputFieldDefinition>
        , IInputFieldDescriptor
    {
        public InputFieldDescriptor(
            IDescriptorContext context,
            NameString fieldName)
            : base(context)
        {
            Definition.Name = fieldName;
        }

        public InputFieldDescriptor(
            IDescriptorContext context,
            PropertyInfo property)
            : base(context)
        {
            Definition.Property = property
                ?? throw new ArgumentNullException(nameof(property));
            Definition.Name = context.Naming.GetMemberName(property);
            Definition.Description =
                context.Naming.GetMemberDescription(property);
            Definition.Type = context.Inspector.GetInputReturnType(property);
        }

        IInputFieldDescriptor IInputFieldDescriptor.SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            base.SyntaxNode(inputValueDefinition);
            return this;
        }

        public IInputFieldDescriptor Name(NameString value)
        {
            Definition.Name = value.EnsureNotEmpty(nameof(value));
            return this;
        }

        public new IInputFieldDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IInputFieldDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        public new IInputFieldDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            base.Type<TInputType>(inputType);
            return this;
        }

        public new IInputFieldDescriptor Type(ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public IInputFieldDescriptor Ignore()
        {
            Definition.Ignore = true;
            return this;
        }

        public new IInputFieldDescriptor DefaultValue(
            IValueNode value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IInputFieldDescriptor DefaultValue(
            object value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IInputFieldDescriptor Directive<TDirective>(
            TDirective directiveInstance)
            where TDirective : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IInputFieldDescriptor Directive<TDirective>()
            where TDirective : class, new()
        {
            base.Directive<TDirective>();
            return this;
        }

        public new IInputFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }
}
