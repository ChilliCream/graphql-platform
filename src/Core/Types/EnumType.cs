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
                    "A type name must not be null or empty.",
                    nameof(config));
            }

            _config = config;
            Name = config.Name;
            Description = config.Description;
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

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes() => Values;

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

        public bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public object ParseLiteral(IValueNode literal, Type targetType)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumTypeConfig
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public EnumTypeDefinitionNode SyntaxNode { get; set; }

        public Func<IEnumerable<EnumValue>> Values { get; set; }
    }
}
