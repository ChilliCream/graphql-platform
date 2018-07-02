using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputField
        : IInputField
    {
        private readonly TypeReference _typeReference;
        private object _nativeDefaultValue;
        private bool _completed;

        internal InputField(ArgumentDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "An input value name must not be null or empty.",
                    nameof(descriptor));
            }

            _typeReference = descriptor.TypeReference;
            _nativeDefaultValue = descriptor.NativeDefaultValue;

            SyntaxNode = descriptor.SyntaxNode;
            Name = descriptor.Name;
            Description = descriptor.Description;
            DefaultValue = descriptor.DefaultValue;
        }

        internal InputField(InputFieldDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "An input value name must not be null or empty.",
                    nameof(descriptor));
            }

            _typeReference = descriptor.TypeReference;
            _nativeDefaultValue = descriptor.NativeDefaultValue;

            SyntaxNode = descriptor.SyntaxNode;
            Name = descriptor.Name;
            Description = descriptor.Description;
            DefaultValue = descriptor.DefaultValue;
            Property = descriptor.Property;
        }

        public InputValueDefinitionNode SyntaxNode { get; }

        public INamedType DeclaringType { get; private set; }

        public string Name { get; }

        public string Description { get; }

        public IInputType Type { get; private set; }

        public IValueNode DefaultValue { get; private set; }

        public PropertyInfo Property { get; private set; }

        #region Initialization

        internal void RegisterDependencies(
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            if (!_completed)
            {
                if (_typeReference != null)
                {
                    typeRegistry.RegisterType(_typeReference);
                }
            }
        }

        internal void CompleteInputField(
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            if (!_completed)
            {
                DeclaringType = parentType;
                Type = this.ResolveFieldType<IInputType>(typeRegistry,
                    reportError, _typeReference);

                if (Type != null)
                {
                    CompleteDefaultValue(Type, reportError, parentType);

                    if (parentType is InputObjectType
                        && Property == null
                        && typeRegistry.TryGetTypeBinding(parentType, out InputObjectTypeBinding binding)
                        && binding.Fields.TryGetValue(Name, out InputFieldBinding fieldBinding))
                    {
                        Property = fieldBinding.Property;
                    }
                }
                _completed = true;
            }
        }

        private void CompleteDefaultValue(
            IInputType type,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            try
            {
                if (DefaultValue == null)
                {
                    if (_nativeDefaultValue == null)
                    {
                        DefaultValue = new NullValueNode();
                    }
                    else
                    {
                        DefaultValue = type.ParseValue(_nativeDefaultValue);
                    }
                }
            }
            catch (Exception ex)
            {
                reportError(new SchemaError(
                    "Could not parse the native value for input field " +
                    $"`{parentType.Name}.{Name}`.", parentType, ex));
            }
        }

        #endregion
    }
}
