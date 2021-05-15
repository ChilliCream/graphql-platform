using System;
using System.Collections.Generic;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class JsonType : ScalarType
    {
        private readonly ObjectValueToDictionaryConverter _objectValueToDictConverter = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonType"/> class.
        /// </summary>
        public JsonType()
            : this(
                  WellKnownScalarTypes.Json,
                  ScalarResources.JsonType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonType"/> class.
        /// </summary>
        public JsonType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
            Description = description;
        }

        public override Type RuntimeType => typeof(JsonDocument);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal switch
            {
                ObjectValueNode or NullValueNode => true,
                _ => false,
            };
        }

        public override object? ParseLiteral(IValueNode literal, bool withDefaults = true)
        {
            return literal switch
            {
                StringValueNode svn => TryDeserializeFromString(svn.Value),
                NullValueNode => null,
                _ => throw ThrowHelper.JsonType_ParseLiteral_IsInvalid(this),
            };
        }

        private object TryDeserializeFromString(string? serialized)
        {
            try
            {
                return JsonDocumentConverter.Convert(JsonSerializer.Deserialize<object>(serialized));
            }
            catch
            {
                throw ThrowHelper.JsonType_ParseLiteral_IsInvalid(this);
            }
        }

        public override IValueNode ParseValue(object? runtimeValue)
        {
            return runtimeValue switch
            {
                null => NullValueNode.Default,

                IReadOnlyDictionary<string, object> dict => ParseValue(dict, new HashSet<object>()),

                JsonElement jsonElement => ParseValue(jsonElement, new HashSet<object>()),

                _ => throw ThrowHelper.JsonType_ParseValue_IsInvalid(this)
            };
        }

        private IValueNode ParseValue(object? value, ISet<object> set)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            switch (value)
            {
                case string s:
                    return new StringValueNode(s);
                case short s:
                    return new IntValueNode(s);
                case int i:
                    return new IntValueNode(i);
                case long l:
                    return new IntValueNode(l);
                case float f:
                    return new FloatValueNode(f);
                case double d:
                    return new FloatValueNode(d);
                case decimal d:
                    return new FloatValueNode(d);
                case bool b:
                    return new BooleanValueNode(b);
                case Guid g:
                    return new StringValueNode(g.ToString());
                case DateTimeOffset d:
                    return new StringValueNode(d.ToString());
            }

            if (set.Add(value))
            {
                if (value is IReadOnlyDictionary<string, object> dict)
                {
                    var fields = new List<ObjectFieldNode>();
                    foreach (KeyValuePair<string, object> field in dict)
                    {
                        fields.Add(new ObjectFieldNode(
                            field.Key,
                            ParseValue(field.Value, set)));
                    }
                    return new ObjectValueNode(fields);
                }

                return ParseValue(JsonDocumentConverter.Convert(value), set);
            }
            
            throw ThrowHelper.JsonType_ParseValue_IsInvalid(this);
        }

        public override IValueNode ParseResult(object? resultValue) => ParseValue(resultValue);

        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue == null)
            {
                resultValue = null;
                return true;
            }

            return Convert(runtimeValue, out resultValue);
        }

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            return Convert(resultValue, out runtimeValue);
        }

        private bool Convert(object? resultValue, out object? runtimeValue)
        {
            object? elementValue;
            runtimeValue = null;
            switch (resultValue)
            {
                case IDictionary<string, object> dictionary:
                    {
                        var result = new Dictionary<string, object?>();
                        foreach (KeyValuePair<string, object> element in dictionary)
                        {
                            if (Convert(element.Value, out elementValue))
                            {
                                result[element.Key] = elementValue;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        runtimeValue = result;
                        return true;
                    }
                case IValueNode literal:
                    runtimeValue = ParseLiteral(literal);
                    return true;
                default:
                    runtimeValue = JsonDocumentConverter.Convert(resultValue);
                    return true;
            }
        }

    }
}
