using System;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InputFieldDescriptor
        : ArgumentDescriptor
        , IInputFieldDescriptor
    {
        public InputFieldDescriptor(string name)
            : base(new InputFieldDescription())
        {
            InputDescription.Name = name;
        }

        public InputFieldDescriptor(PropertyInfo property)
            : base(new InputFieldDescription())
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            InputDescription.Property = property;
            InputDescription.Name = property.GetGraphQLName();
            InputDescription.TypeReference = new TypeReference(property.PropertyType);
        }

        protected new InputFieldDescription InputDescription
            => (InputFieldDescription)base.InputDescription;

        public new InputFieldDescription CreateInputDescription()
        {
            return InputDescription;
        }

        protected void Name(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The input field name cannot be null or empty.",
                    nameof(name));
            }

            if (!ValidationHelper.IsFieldNameValid(name))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL input field name.",
                    nameof(name));
            }

            InputDescription.Name = name;
        }

        #region IInputFieldDescriptor

        IInputFieldDescriptor IInputFieldDescriptor.SyntaxNode(
            InputValueDefinitionNode syntaxNode)
        {
            SyntaxNode(syntaxNode);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Name(string name)
        {
            Name(name);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Description(
            string description)
        {
            Description(description);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Type<TInputType>()
        {
            Type<TInputType>();
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Type(ITypeNode type)
        {
            Type(type);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(
            IValueNode defaultValue)
        {
            DefaultValue(defaultValue);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(
            object defaultValue)
        {
            DefaultValue(defaultValue);
            return this;
        }

        #endregion
    }
}
