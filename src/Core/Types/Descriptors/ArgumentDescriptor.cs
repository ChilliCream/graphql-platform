using System;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ArgumentDescriptor
        : IArgumentDescriptor
    {
        public ArgumentDescriptor(string argumentName, Type argumentType)
            : this(argumentName)
        {
            if (argumentType == null)
            {
                throw new ArgumentNullException(nameof(argumentType));
            }

            TypeReference = new TypeReference(argumentType);
            DefaultValue = new NullValueNode();
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

            Name = argumentName;
            DefaultValue = new NullValueNode();
        }

        public InputValueDefinitionNode SyntaxNode { get; protected set; }
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public TypeReference TypeReference { get; protected set; }
        public IValueNode DefaultValue { get; protected set; }
        public object NativeDefaultValue { get; protected set; }

        #region IArgumentDescriptor

        IArgumentDescriptor IArgumentDescriptor.SyntaxNode(
            InputValueDefinitionNode syntaxNode)
        {
            SyntaxNode = syntaxNode;
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Description(string description)
        {
            Description = description;
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Type<TInputType>()
        {
            TypeReference = TypeReference.GetMoreSpecific(typeof(TInputType));
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Type(ITypeNode type)
        {
            TypeReference = TypeReference.GetMoreSpecific(type);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.DefaultValue(IValueNode valueNode)
        {
            DefaultValue = valueNode ?? new NullValueNode();
            NativeDefaultValue = null;
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.DefaultValue(object defaultValue)
        {
            if (defaultValue == null)
            {
                DefaultValue = new NullValueNode();
                NativeDefaultValue = null;
            }
            else
            {
                NativeDefaultValue = defaultValue;
                DefaultValue = null;
            }
            return this;
        }

        #endregion
    }
}
