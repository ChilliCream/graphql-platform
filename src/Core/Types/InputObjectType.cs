using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputObjectType
        : INamedType
        , IInputType
        , INullableType
        , ITypeSystemNode
        , INeedsInitialization
    {
        public readonly Dictionary<string, InputField> _fieldMap =
            new Dictionary<string, InputField>();
        private readonly Func<SchemaContext, Type> _nativeTypeFactory;
        private Type _nativeType;
        private Func<ObjectValueNode, object> _deserialize;
        private bool _hasDeserializer;

        internal InputObjectType(InputObjectTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "An input object type name must not be null or empty.",
                    nameof(config));
            }

            InputField[] fields = config.Fields?.ToArray()
                ?? Array.Empty<InputField>();

            if (fields.Length == 0)
            {
                throw new ArgumentException(
                   $"The input object `{config.Name}` must at least " +
                   "provide one field.",
                   nameof(config));
            }

            foreach (InputField field in fields)
            {
                if (_fieldMap.ContainsKey(field.Name))
                {
                    throw new ArgumentException(
                        $"The input field name `{field.Name}` " +
                        $"is not unique within `{config.Name}`.",
                        nameof(config));
                }
                else
                {
                    _fieldMap.Add(field.Name, field);
                }
            }

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        public InputObjectTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyDictionary<string, InputField> Fields => _fieldMap;

        public Type NativeType => _nativeType;

        public bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is ObjectValueNode
                || literal is NullValueNode;
        }

        public object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (_deserialize == null)
            {
                throw new InvalidOperationException(
                    "No deserializer was configured for " +
                    $"input object type `{Name}`.");
            }

            if (literal is ObjectValueNode ov)
            {
                return _deserialize(ov);
            }

            throw new ArgumentException(
                "The input object type can only parse object value literals.",
                nameof(literal));
        }

        private object ParseLiteralWithParser(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is ObjectValueNode objectLiteral)
            {
                if (!_hasDeserializer)
                {
                    throw new InvalidOperationException(
                        "There is no deserializer availabel for input " +
                        $"object type `{Name}`");
                }
                return _deserialize(objectLiteral);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                "The string type can only parse string literals.",
                nameof(literal));
        }

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            return Fields.Values;
        }

        #endregion

        #region Initialization

        void INeedsInitialization.CompleteInitialization(
            SchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            _nativeType = _nativeTypeFactory(schemaContext);
            if (_nativeType == null)
            {
                reportError(new SchemaError(
                    "Could not resolve the native type associated with " +
                    $"input object type `{Name}`.",
                    this));
            }
            else
            {
                _deserialize = InputObjectDeserializerDiscoverer.Discover(
                    schemaContext, reportError, this, _nativeType);
            }

            foreach (InputField field in _fieldMap.Values)
            {
                field.CompleteInitialization(schemaContext, reportError, this);
            }
        }

        #endregion
    }
}
