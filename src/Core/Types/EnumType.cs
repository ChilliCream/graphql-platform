using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class EnumType
        : INamedType
        , IOutputType
        , IInputType
        , INullableType
        , ITypeSystemNode
        , ITypeInitializer
    {
        private readonly IEnumerable<EnumValue> _values;
        private Dictionary<string, EnumValue> _nameToValues;
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
                    "Am enum type name must not be null or empty.",
                    nameof(config));
            }

            if (config.Values == null)
            {
                throw new ArgumentException(
                    "An enum type must provide enum values.",
                    nameof(config));
            }

            _values = config.Values;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }
        public EnumTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyCollection<EnumValue> Values => _nameToValues.Values;

        public Type NativeType { get; internal set; }

        public bool TryGetValue(string name, out object value)
        {
            if (_nameToValues.TryGetValue(name, out var enumValue))
            {
                value = enumValue.Value;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetName(object value, out string name)
        {
            if (_valueToValues.TryGetValue(value, out var enumValue))
            {
                name = enumValue.Name;
                return true;
            }

            name = null;
            return false;
        }

        public bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is EnumValueNode ev)
            {
                return _nameToValues.ContainsKey(ev.Value);
            }
            return false;
        }

        public object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is EnumValueNode evn
                && _nameToValues.TryGetValue(evn.Value, out EnumValue ev))
            {
                return ev.Value;
            }

            throw new ArgumentException(
                "The specified value cannot be handled " +
                $"by the EnumType {Name}.");
        }

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes() => Values;

        #endregion

        #region Initialization

        void ITypeInitializer.CompleteInitialization(Action<SchemaError> reportError)
        {
            try
            {
                EnumValue[] values = _values.ToArray();
                if (values.Length == 0)
                {
                    reportError(new SchemaError(
                        $"The enum type {Name} has no values.",
                        this));
                }
                else
                {
                    NativeType = values.First(t => t.Value != null).Value.GetType();
                }

                _nameToValues = values.ToDictionary(t => t.Name);
                _valueToValues = values.ToDictionary(t => t.Value);

                // TODO : what to do if:
                // - values are not of the same type
                // - one or more values are null
            }
            catch
            {
                reportError(new SchemaError(
                    $"The enum values of {Name} are not unique.",
                    this));
            }
        }

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
