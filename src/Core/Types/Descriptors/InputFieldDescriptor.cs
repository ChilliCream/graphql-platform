using System;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class InputFieldDescriptor
        : IInputFieldDescriptor
    {
        public InputFieldDescriptor(string name)
        {
            Name = name;
        }

        public InputFieldDescriptor(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Property = property;
            Name = property.GetGraphQLName();
            TypeReference = new TypeReference(property.PropertyType);
        }

        public PropertyInfo Property { get; }

        public InputValueDefinitionNode SyntaxNode { get; protected set; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public TypeReference TypeReference { get; protected set; }

        public IValueNode DefaultValue { get; protected set; }

        public object NativeDefaultValue { get; protected set; }

        #region IInputFieldDescriptor

        IInputFieldDescriptor IInputFieldDescriptor.SyntaxNode(InputValueDefinitionNode syntaxNode)
        {
            SyntaxNode = syntaxNode;
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Name(string name)
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

            Name = name;
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Description(string description)
        {
            Description = description;
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Type<TInputType>()
        {
            TypeReference = TypeReference.GetMoreSpecific(typeof(TInputType));
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Type(ITypeNode type)
        {
            TypeReference = TypeReference.GetMoreSpecific(type);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(IValueNode defaultValue)
        {
            DefaultValue = defaultValue ?? new NullValueNode();
            NativeDefaultValue = null;
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(object defaultValue)
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
