using System.Reflection.Emit;
using System;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public class ArgumentDescriptor
        : ArgumentDescriptorBase<ArgumentDefinition>
        , IArgumentDescriptor
    {
        protected ArgumentDescriptor(ArgumentDefinition argumentDefinition)
        {
            Definition = argumentDefinition
                ?? throw new ArgumentNullException(nameof(argumentDefinition));
        }

        public ArgumentDescriptor(string argumentName, Type argumentType)
            : this(argumentName)
        {
            if (argumentType == null)
            {
                throw new ArgumentNullException(nameof(argumentType));
            }

            Definition = new ArgumentDefinition();
            Definition.Name = argumentName;
            Definition.Type = argumentType.GetInputType();
            Definition.DefaultValue = NullValueNode.Default;
        }

        public ArgumentDescriptor(NameString argumentName)
        {
            Definition = new ArgumentDefinition
            {
                Name = argumentName.EnsureNotEmpty(nameof(argumentName)),
                DefaultValue = NullValueNode.Default
            };
        }

        protected override ArgumentDefinition Definition { get; }

        public new IArgumentDescriptor SyntaxNode(
            InputValueDefinitionNode inputValueDefinition)
        {
            base.SyntaxNode(inputValueDefinition);
            return this;
        }

        public new IArgumentDescriptor Description(string value)
        {
            base.Description(value);
            return this;
        }

        public new IArgumentDescriptor Type<TInputType>()
            where TInputType : IInputType
        {
            base.Type<TInputType>();
            return this;
        }

        public new IArgumentDescriptor Type<TInputType>(
            TInputType inputType)
            where TInputType : class, IInputType
        {
            base.Type<TInputType>(inputType);
            return this;
        }

        public new IArgumentDescriptor Type(
            ITypeNode typeNode)
        {
            base.Type(typeNode);
            return this;
        }

        public new IArgumentDescriptor DefaultValue(IValueNode value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IArgumentDescriptor DefaultValue(object value)
        {
            base.DefaultValue(value);
            return this;
        }

        public new IArgumentDescriptor Directive<T>(T directiveInstance)
            where T : class
        {
            base.Directive(directiveInstance);
            return this;
        }

        public new IArgumentDescriptor Directive<T>()
            where T : class, new()
        {
            base.Directive<T>();
            return this;
        }

        public new IArgumentDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            base.Directive(name, arguments);
            return this;
        }
    }


}
