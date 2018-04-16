using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputField
        : ITypeSystemNode
    {
        private readonly InputFieldConfig _config;
        private IInputType _type;
        private object _defaultValue;
        private bool _isDefaultValueResolved;

        public InputField(InputFieldConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "A input value name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        public InputValueDefinitionNode SyntaxNode { get; }
        public string Name { get; }
        public string Description { get; }
        public IInputType Type
        {
            get
            {
                if (_type == null)
                {
                    _type = _config.Type();
                    if (_type == null)
                    {
                        throw new InvalidOperationException(
                            "An input field always has to specify a value type.");
                    }
                }
                return _type;
            }
        }

        public object DefaultValue
        {
            get
            {
                if (!_isDefaultValueResolved)
                {
                    _defaultValue = _config.DefaultValue();
                    _isDefaultValueResolved = true;
                }
                return _defaultValue;
            }
        }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes() =>
            Enumerable.Empty<ITypeSystemNode>();
    }

    public class InputFieldConfig
    {
        public InputValueDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public Func<IInputType> Type { get; set; }

        public Func<object> DefaultValue { get; set; }
    }
}