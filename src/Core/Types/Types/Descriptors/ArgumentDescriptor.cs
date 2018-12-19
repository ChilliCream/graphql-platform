using System;
using HotChocolate.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ArgumentDescriptor
        : IArgumentDescriptor
        , IDescriptionFactory<ArgumentDescription>
    {
        protected ArgumentDescriptor(ArgumentDescription argumentDescription)
        {
            InputDescription = argumentDescription
                ?? throw new ArgumentNullException(nameof(argumentDescription));
        }

        public ArgumentDescriptor(string argumentName, Type argumentType)
            : this(argumentName)
        {
            if (argumentType == null)
            {
                throw new ArgumentNullException(nameof(argumentType));
            }

            InputDescription = new ArgumentDescription();
            InputDescription.Name = argumentName;
            InputDescription.TypeReference = argumentType.GetInputType();
            InputDescription.DefaultValue = NullValueNode.Default;
        }

        public ArgumentDescriptor(NameString argumentName)
        {
            InputDescription = new ArgumentDescription
            {
                Name = argumentName.EnsureNotEmpty(nameof(argumentName)),
                DefaultValue = NullValueNode.Default
            };
        }

        protected ArgumentDescription InputDescription { get; }

        public ArgumentDescription CreateDescription()
        {
            return InputDescription;
        }

        protected void SyntaxNode(InputValueDefinitionNode syntaxNode)
        {
            InputDescription.SyntaxNode = syntaxNode;

        }
        protected void Description(string description)
        {
            InputDescription.Description = description;
        }

        protected void Type<TInputType>()
        {
            InputDescription.TypeReference = InputDescription.TypeReference
                .GetMoreSpecific(typeof(TInputType), TypeContext.Input);
        }

        protected void Type(ITypeNode type)
        {
            InputDescription.TypeReference = InputDescription.TypeReference
                .GetMoreSpecific(type);
        }

        protected void DefaultValue(IValueNode defaultValue)
        {
            InputDescription.DefaultValue =
                defaultValue ?? NullValueNode.Default;
            InputDescription.NativeDefaultValue = null;
        }

        protected void DefaultValue(object defaultValue)
        {
            if (defaultValue == null)
            {
                InputDescription.DefaultValue = NullValueNode.Default;
                InputDescription.NativeDefaultValue = null;
            }
            else
            {
                InputDescription.TypeReference = InputDescription.TypeReference
                    .GetMoreSpecific(defaultValue.GetType(), TypeContext.Input);
                InputDescription.NativeDefaultValue = defaultValue;
                InputDescription.DefaultValue = null;
            }
        }

        #region IArgumentDescriptor

        IArgumentDescriptor IArgumentDescriptor.SyntaxNode(
            InputValueDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Type<TInputType>()
        {
            Type<TInputType>();
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Type(
            ITypeNode type)
        {
            Type(type);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.DefaultValue(
            IValueNode defaultValue)
        {
            DefaultValue(defaultValue);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.DefaultValue(
            object defaultValue)
        {
            DefaultValue(defaultValue);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Directive<T>(
            T directive)
        {
            InputDescription.Directives.AddDirective(directive);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Directive<T>()
        {
            InputDescription.Directives.AddDirective(new T());
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Directive(
            NameString name,
            params ArgumentNode[] arguments)
        {
            InputDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Directive(
            string name,
            params ArgumentNode[] arguments)
        {
            InputDescription.Directives.AddDirective(name, arguments);
            return this;
        }

        #endregion
    }
}
