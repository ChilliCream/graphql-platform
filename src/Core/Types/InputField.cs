using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputField
        : ITypeSystemNode
    {
        private readonly TypeReference _typeReference;
        private object _nativeDefaultValue;

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
        }

        public InputValueDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IInputType Type { get; private set; }

        public IValueNode DefaultValue { get; private set; }

        public PropertyInfo Property { get; private set; }

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes() =>
            Enumerable.Empty<ITypeSystemNode>();

        #endregion

        #region Initialization

        internal void RegisterDependencies(
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            if (_typeReference != null)
            {
                typeRegistry.RegisterType(_typeReference);
            }
        }

        internal void CompleteInputField(
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            CompleteType(typeRegistry, reportError, parentType);
            CompleteDefaultValue(Type, reportError, parentType);

            if (parentType is InputObjectType
                && Property == null
                && typeRegistry.TryGetTypeBinding(parentType, out InputObjectTypeBinding binding)
                && binding.Fields.TryGetValue(Name, out InputFieldBinding fieldBinding))
            {
                Property = fieldBinding.Property;
            }
        }

        private void CompleteType(
           ITypeRegistry typeRegistry,
           Action<SchemaError> reportError,
           INamedType parentType)
        {
            if (_typeReference != null)
            {
                Type = typeRegistry.GetType<IInputType>(_typeReference);
            }

            if (Type == null)
            {
                reportError(new SchemaError(
                    $"The type of field `{parentType.Name}.{Name}` is null.",
                    parentType));
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
