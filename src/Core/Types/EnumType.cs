using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class EnumType
        : IOutputType
        , IInputType
        , INamedType
        , INullableType
        , ITypeSystemNode
    {
        private readonly EnumTypeConfig _config;
        private readonly ParseLiteral _parseLiteral;
        private Dictionary<string, EnumValue> _nameTovalues;
        private Dictionary<object, EnumValue> _valueToValues;

        public EnumType(EnumTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "An type name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            Name = config.Name;
            Description = config.Description;
            _parseLiteral = config.ParseLiteral;
        }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyCollection<EnumValue> Values
        {
            get
            {
                InitializeValues();
                return _nameTovalues.Values;
            }
        }

        public EnumTypeDefinitionNode SyntaxNode { get; }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        public IEnumerable<ITypeSystemNode> GetNodes()
        {
            return Values;
        }

        public bool TryGetValue(string name, out object value)
        {
            InitializeValues();

            if (_nameTovalues.TryGetValue(name, out var enumValue))
            {
                value = enumValue.Value;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetName(object value, out string name)
        {
            InitializeValues();

            if (_valueToValues.TryGetValue(value, out var enumValue))
            {
                name = enumValue.Name;
                return true;
            }

            name = null;
            return false;
        }

        // .net native to external  
        public string Serialize(object value)
        {
            return _valueToValues[value].Name;
        }

        // ast node to .net native
        public object ParseLiteral(IValueNode value, GetVariableValue getVariableValue)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (getVariableValue == null)
            {
                throw new ArgumentNullException(nameof(getVariableValue));
            }

            return _parseLiteral(value, getVariableValue);
        }

        private void InitializeValues()
        {
            if (_nameTovalues == null || _valueToValues == null)
            {
                var values = _config.Values();
                if (values == null)
                {
                    throw new InvalidOperationException(
                        "An enum type must have at least one value.");
                }
                _nameTovalues = values.ToDictionary(t => t.Name);
                _valueToValues = values.ToDictionary(t => t.Value);
            }
        }
    }

    public class EnumTypeConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public EnumTypeDefinitionNode SyntaxNode { get; set; }

        public Func<IEnumerable<EnumValue>> Values { get; set; }

        public ParseLiteral ParseLiteral { get; set; }
    }
}