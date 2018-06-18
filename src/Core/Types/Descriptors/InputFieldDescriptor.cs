using System;
using System.Reflection;
using HotChocolate.Configuration;
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
            NativeType = property.PropertyType;
        }

        public PropertyInfo Property { get; }

        public InputValueDefinitionNode SyntaxNode { get; protected set; }

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public Type NativeType { get; protected set; }

        public ITypeNode Type { get; protected set; }

        public IValueNode DefaultValue { get; protected set; }

        public object NativeDefaultValue { get; protected set; }

        public InputField CreateField()
        {
            return new InputField(new InputFieldConfig
            {
                Name = Name,
                Description = Description,
                Property = Property,
                NativeNamedType = TypeInspector.Default.ExtractNamedType(NativeType),
                Type = CreateType,
                DefaultValue = CreateDefaultValue
            });
        }

        private IInputType CreateType(ITypeRegistry typeRegistry)
        {
            return TypeInspector.Default.CreateInputType(
                typeRegistry, NativeType);
        }

        private IValueNode CreateDefaultValue(ITypeRegistry typeRegistry)
        {
            if (DefaultValue != null)
            {
                return DefaultValue;
            }

            if (NativeDefaultValue != null)
            {
                Type nativeNamedType = TypeInspector.Default.ExtractNamedType(NativeType);
                IType type = typeRegistry.GetType<IType>(nativeNamedType);
                if (type is IInputType inputType)
                {
                    return inputType.ParseValue(NativeDefaultValue);
                }
            }

            return new NullValueNode();
        }

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
            NativeType = typeof(TInputType);
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.Type(ITypeNode type)
        {
            Type = type;
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(IValueNode defaultValue)
        {
            DefaultValue = defaultValue;
            NativeDefaultValue = null;
            return this;
        }

        IInputFieldDescriptor IInputFieldDescriptor.DefaultValue(object defaultValue)
        {
            DefaultValue = null;
            NativeDefaultValue = defaultValue;
            return this;
        }

        #endregion
    }
}
