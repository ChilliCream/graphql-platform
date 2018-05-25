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
        private readonly Func<IInputType> _typeFactory;
        private readonly Func<IValueNode> _defaultValueFactory;
        private IInputType _type;
        private IValueNode _defaultValue;

        public InputField(InputFieldConfig config)
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
            _defaultValueFactory = config.DefaultValue;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        public InputValueDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IInputType Type => _type;

        public IValueNode DefaultValue => _defaultValue;

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes() =>
            Enumerable.Empty<ITypeSystemNode>();

        #endregion

        #region Initialization

        internal void CompleteInitialization(
            Action<SchemaError> reportError,
            INamedType parentType)
        {
            _type = _typeFactory();
            if (_type == null)
            {
                reportError(new SchemaError(
                    $"The type of the input field {Name} is null.",
                    parentType));
            }


            if (_defaultValueFactory == null)
            {
                _defaultValue = new NullValueNode(null);
            }
            else
            {
                _defaultValue = _defaultValueFactory();
            }
        }

        #endregion
    }

    public class InputFieldConfig
    {
        public InputValueDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IInputType> Type { get; set; }

        public Func<IValueNode> DefaultValue { get; set; }
    }
}
