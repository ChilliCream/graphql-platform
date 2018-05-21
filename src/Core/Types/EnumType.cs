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

        public Type NativeType => throw new NotImplementedException();

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

        public object ParseLiteral(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes() => Values;

        #endregion
    }

    public class EnumTypeConfig
        : INamedTypeConfig
    {
        public EnumTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsIntrospection { get; set; }

        public IEnumerable<EnumValue> Values { get; set; }

        public virtual Type NativeType { get; set; }
    }

    public class EnumTypeConfig<T>
        : EnumTypeConfig
    {
        public new IEnumerable<EnumValue<T>> Values { get; set; }

        public override Type NativeType
        {
            get => typeof(T);
            set => throw new NotSupportedException();
        }
    }
}
