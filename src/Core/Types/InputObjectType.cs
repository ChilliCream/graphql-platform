using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputObjectType
        : INamedType
        , IInputType
        , INullableType
        , ITypeSystemNode
        , ITypeInitializer
    {
        public readonly Dictionary<string, InputField> _fieldMap =
            new Dictionary<string, InputField>();
        private readonly Func<Type> _nativeTypeFactory;
        private Type _nativeType;
        private Func<ObjectValueNode, object> _deserialize;
        private bool _hasDeserializer;

        public InputObjectType(InputObjectTypeConfig config)
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
            object o = Activator.CreateInstance(_nativeType);
            //    foreach ()

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

        void ITypeInitializer.CompleteInitialization(
            SchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            _nativeType = _nativeTypeFactory();
            if (_nativeType == null)
            {
                reportError(new SchemaError(
                    "Could not resolve the native type associated with " +
                    $"input object type `{Name}`.",
                    this));
            }
            else
            {
                CreateNativeTypeDeserializer(_nativeType, reportError);
            }

            foreach (InputField field in _fieldMap.Values)
            {
                field.CompleteInitialization(reportError, this);
            }
        }

        private void CreateNativeTypeDeserializer(
            Type nativeType,
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            if (!TryCreateNativeTypeParserDeserializer(
                    nativeType, reportError, out _deserialize)
                && !TryCreateNativeConstructorDeserializer(
                    nativeType, out _deserialize)
                && !TryCreateNativeReflectionDeserializer(
                    nativeType, schemaContext, out _deserialize))
            {
                reportError(new SchemaError(
                    "Could not create a literal parser for input " +
                    $"object type `{Name}`", this));
            }
        }

        private bool TryCreateNativeTypeParserDeserializer(
            Type nativeType,
            Action<SchemaError> reportError,
            out Func<ObjectValueNode, object> deserializer)
        {
            if (nativeType.IsDefined(typeof(GraphQLLiteralParserAttribute)))
            {
                Type parserType = nativeType
                    .GetCustomAttribute<GraphQLLiteralParserAttribute>().Type;
                if (typeof(ILiteralParser).IsAssignableFrom(parserType))
                {
                    ILiteralParser parser = (ILiteralParser)Activator
                        .CreateInstance(parserType);
                    deserializer = literal => parser.ParseLiteral(literal);
                    return true;
                }
                else
                {
                    reportError(new SchemaError(
                        "A literal parser has to implement `ILiteralParser`.",
                        this));
                }
            }

            deserializer = null;
            return false;
        }

        private static bool TryCreateNativeConstructorDeserializer(
            Type nativeType,
            out Func<ObjectValueNode, object> deserializer)
        {
            ConstructorInfo nativeTypeConstructor =
                nativeType.GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic)
                .Where(t => t.GetParameters().Length == 1)
                .FirstOrDefault(t => t.GetParameters()
                    .First().ParameterType == nativeType);

            if (nativeTypeConstructor != null)
            {
                deserializer = literal => nativeTypeConstructor
                    .Invoke(new object[] { literal });
                return true;
            }

            deserializer = null;
            return false;
        }

        private bool TryCreateNativeReflectionDeserializer(
            Type nativeType,
            SchemaContext schemaContext,
            out Func<ObjectValueNode, object> deserializer)
        {
            ConstructorInfo nativeTypeConstructor =
                nativeType.GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(t => t.GetParameters().Length == 0);
            if (nativeTypeConstructor != null)
            {
                deserializer = literal => InputObjectDefaultDeserializer
                    .ParseLiteral(this, nativeType, literal);
                return true;
            }

            deserializer = null;
            return false;
        }

        #endregion
    }

    public class InputObjectTypeConfig
        : INamedTypeConfig
    {
        public InputObjectTypeDefinitionNode SyntaxNode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<InputField> Fields { get; set; }

        public Func<Type> NativeType { get; set; }
    }

    internal static class InputObjectDefaultDeserializer
    {
        public static object ParseLiteral(
            SchemaContext schemaContext,
            InputObjectType inputObjectType,
            ObjectValueNode literal)
        {
            Dictionary<string, IValueNode> fieldValues = literal.Fields
                .ToDictionary(t => t.Name.Value, t => t.Value);

            Dictionary<string, PropertyInfo> properties = schemaContext
                .GetNativeTypeMembers(inputObjectType.Name)
                .ToDictionary(t => t.FieldName, t => (PropertyInfo)t.Member);

            object nativeInputObject = Activator.CreateInstance(
                inputObjectType.NativeType);

            foreach (InputField field in inputObjectType.Fields.Values)
            {
                if (fieldValues.TryGetValue(field.Name, out IValueNode value))
                {
                    DeserializeProperty(properties, field, literal, nativeInputObject);
                }
                else if (field.DefaultValue != null)
                {
                    if (field.DefaultValue is NullValueNode && field.Type.IsNonNullType())
                    {
                        // TODO : thorw type deserialization exception -> InputObjectTypeDeserializationException
                    }
                    DeserializeProperty(properties, field, literal, nativeInputObject);
                }
                else if (field.Type.IsNonNullType())
                {
                    // TODO : thorw type deserialization exception -> InputObjectTypeDeserializationException
                }
            }

            return nativeInputObject;
        }

        private static void DeserializeProperty(
            Dictionary<string, PropertyInfo> properties,
            InputField field,
            IValueNode literal,
            object nativeInputObject)
        {
            if (properties.TryGetValue(field.Name, out PropertyInfo property))
            {
                if (property.PropertyType.IsAssignableFrom(field.Type.NativeType))
                {
                    property.SetValue(nativeInputObject, field.Type.ParseLiteral(literal));
                }
                else
                {
                    // TODO : thorw type deserialization exception -> InputObjectTypeDeserializationException
                }
            }
            else
            {
                // TODO : thorw type deserialization exception -> InputObjectTypeDeserializationException
            }
        }
    }
}
