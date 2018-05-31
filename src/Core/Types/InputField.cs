using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputField
        : ITypeSystemNode
    {
        private readonly Func<ITypeRegistry, IInputType> _typeFactory;
        private readonly Func<ITypeRegistry, IValueNode> _defaultValueFactory;
        private IInputType _type;
        private Type _nativeNamedType;

        internal InputField(InputFieldConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "An input value name must not be null or empty.",
                    nameof(config));
            }

            if (config.Type == null)
            {
                throw new ArgumentException(
                    "An input type factory must not be null or empty.",
                    nameof(config));
            }

            _typeFactory = config.Type;
            _nativeNamedType = config.NativeNamedType;
            _defaultValueFactory = config.DefaultValue;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
            Property = config.Property;
        }

        public InputValueDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IInputType Type => _type;

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
            if (_nativeNamedType != null)
            {
                typeRegistry.RegisterType(_nativeNamedType);
            }
        }

        internal void CompleteInputField(
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            _type = _typeFactory(typeRegistry);
            if (_type == null)
            {
                reportError(new SchemaError(
                    $"The type of the input field {Name} is null.",
                    parentType));
            }

            if (_defaultValueFactory == null)
            {
                DefaultValue = new NullValueNode(null);
            }
            else
            {
                DefaultValue = _defaultValueFactory(typeRegistry);
            }

            if (parentType is InputObjectType
                && Property == null
                && typeRegistry.TryGetTypeBinding(parentType, out ITypeBinding binding)
                && binding.Members.TryGetValue(Name, out TypeMemberBinding memberBinding)
                && memberBinding.Member is PropertyInfo p)
            {
                Property = p;
            }
        }

        #endregion
    }
}
