using System;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal class ArgumentDescriptor
        : IArgumentDescriptor
    {
        public ArgumentDescriptor(string argumentName)
        {
            if (string.IsNullOrEmpty(argumentName))
            {
                throw new ArgumentException(
                    "The argument name cannot be null or empty.",
                    nameof(argumentName));
            }

            if (ValidationHelper.IsFieldNameValid(argumentName))
            {
                throw new ArgumentException(
                    "The specified name is not a valid GraphQL argument name.",
                    nameof(argumentName));
            }

            Name = argumentName;
        }

        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public Type NativeType { get; protected set; }
        public IValueNode DefaultValue { get; protected set; }
        public object NativeDefaultValue { get; protected set; }

        public InputField CreateArgument()
        {
            return new InputField(new InputFieldConfig
            {
                Name = Name,
                Description = Description,
                NativeNamedType = TypeInspector.Default.ExtractNamedType(NativeType),
                Type = CreateType,
                DefaultValue = CreateValue
            });
        }

        private IInputType CreateType(ITypeRegistry typeRegistry)
        {
            return TypeInspector.Default.CreateInputType(
                typeRegistry, NativeType);
        }

        private IValueNode CreateValue(ITypeRegistry typeRegistry)
        {
            if (DefaultValue == null)
            {
                return DefaultValue;
            }
            else
            {
                IInputType type = CreateType(typeRegistry);
                return type.ParseValue(NativeDefaultValue);
            }
        }

        #region IArgumentDescriptor

        IArgumentDescriptor IArgumentDescriptor.Description(string description)
        {
            Description = description;
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.Type<TInputType>()
        {
            NativeType = typeof(TInputType);
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.DefaultValue(IValueNode valueNode)
        {
            if (valueNode == null)
            {
                throw new ArgumentNullException(nameof(valueNode));
            }

            DefaultValue = valueNode;
            NativeDefaultValue = null;
            return this;
        }

        IArgumentDescriptor IArgumentDescriptor.DefaultValue(object defaultValue)
        {
            NativeDefaultValue = defaultValue;
            DefaultValue = null;
            return this;
        }

        #endregion
    }
}
