using System;
using HotChocolate.Internal;
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
            InputDescription.TypeReference = new TypeReference(argumentType);
            InputDescription.DefaultValue = new NullValueNode();
        }

        public ArgumentDescriptor(string argumentName)
        {
            if (string.IsNullOrEmpty(argumentName))
            {
                throw new ArgumentException(
                    "The argument name cannot be null or empty.",
                    nameof(argumentName));
            }

            if (!ValidationHelper.IsFieldNameValid(argumentName))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL argument name.",
                    nameof(argumentName));
            }

            InputDescription = new ArgumentDescription();
            InputDescription.Name = argumentName;
            InputDescription.DefaultValue = new NullValueNode();
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
                .GetMoreSpecific(typeof(TInputType));
        }

        protected void Type(ITypeNode type)
        {
            InputDescription.TypeReference = InputDescription.TypeReference
                .GetMoreSpecific(type);
        }

        protected void DefaultValue(IValueNode valueNode)
        {
            InputDescription.DefaultValue = valueNode ?? new NullValueNode();
            InputDescription.NativeDefaultValue = null;
        }

        protected void DefaultValue(object defaultValue)
        {
            if (defaultValue == null)
            {
                InputDescription.DefaultValue = new NullValueNode();
                InputDescription.NativeDefaultValue = null;
            }
            else
            {
                InputDescription.TypeReference = InputDescription.TypeReference
                    .GetMoreSpecific(defaultValue.GetType());
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

        IArgumentDescriptor IArgumentDescriptor.Description(string description)
        {
            Description(description);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Type<TInputType>()
        {
            Type<TInputType>();
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Type(ITypeNode type)
        {
            Type(type);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.DefaultValue(IValueNode valueNode)
        {
            DefaultValue(valueNode);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.DefaultValue(object defaultValue)
        {
            DefaultValue(defaultValue);
            return this;
        }

        #endregion
    }
}
