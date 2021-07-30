using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using HotChocolate.Utilities.Serialization;
using static HotChocolate.Utilities.Serialization.InputObjectParserHelper;
using static HotChocolate.Utilities.Serialization.InputObjectConstructorResolver;
using static HotChocolate.Utilities.Serialization.InputObjectCompiler;

#nullable enable

namespace HotChocolate.Types
{
    public partial class InputObjectType : ISerializableType2, IParsableType2
    {
        public object? Serialize(object? runtimeValue, Path? path)
        {
            throw new NotImplementedException();
        }

        public object? Deserialize(object? resultValue, Path? path)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public virtual object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is null)
            {
                throw new ArgumentNullException(nameof(valueSyntax));
            }

            if (valueSyntax is ObjectValueNode objectValueSyntax)
            {
                return _parseLiteral(objectValueSyntax);
            }

            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            throw new SerializationException(
                TypeResources.InputObjectType_CannotParseLiteral,
                this);
        }

        /// <inheritdoc />
        public virtual IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return NullValueNode.Default;
            }

            return _objectToValueConverter.Convert(this, runtimeValue);
        }

        /// <inheritdoc />
        public IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is IReadOnlyDictionary<string, object> dict)
            {
                var list = new List<ObjectFieldNode>();

                foreach (InputField field in Fields)
                {
                    if(dict.TryGetValue(field.Name.Value, out var value))
                    {
                        list.Add(new ObjectFieldNode(
                            field.Name.Value,
                            field.Type.ParseResult(value)));
                    }
                }

                return new ObjectValueNode(list);
            }

            if (RuntimeType != typeof(object) && RuntimeType.IsInstanceOfType(resultValue))
            {
                return ParseValue(resultValue);
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
                this);
        }

        /// <inheritdoc />
        public object? Serialize(object? runtimeValue)
        {
            if (TrySerialize(runtimeValue, out var serialized))
            {
                return serialized;
            }

            throw new SerializationException(
                "The specified value is not a valid input object.",
                this);
        }

        public virtual bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            try
            {
                if (runtimeValue is null)
                {
                    resultValue = null;
                    return true;
                }

                if (runtimeValue is IReadOnlyDictionary<string, object> ||
                    runtimeValue is IDictionary<string, object>)
                {
                    resultValue = runtimeValue;
                    return true;
                }

                resultValue = _objectToDictionary.Convert(this, runtimeValue);
                return true;
            }
            catch
            {
                resultValue = null;
                return false;
            }
        }

        /// <inheritdoc />
        public object? Deserialize(object? resultValue)
        {
            if (TryDeserialize(resultValue, out var deserialized))
            {
                return deserialized;
            }

            throw new SerializationException(
                "The specified value is not a serialized input object.",
                this);
        }

        public virtual bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            try
            {
                if (resultValue is null)
                {
                    runtimeValue = null;
                    return true;
                }

                if (resultValue is IReadOnlyDictionary<string, object> dict)
                {
                    runtimeValue = _deserialize(dict);
                    return true;
                }

                if (RuntimeType != typeof(object) && RuntimeType.IsInstanceOfType(resultValue))
                {
                    runtimeValue = resultValue;
                    return true;
                }

                runtimeValue = null;
                return false;
            }
            catch
            {
                runtimeValue = null;
                return false;
            }
        }

        public object? ParseLiteral(IValueNode valueSyntax, Path path, bool withDefaults = true)
        {
            throw new NotImplementedException();
        }

        public IValueNode ParseValue(object? runtimeValue, Path path)
        {
            throw new NotImplementedException();
        }

        public IValueNode ParseResult(object? resultValue, Path path)
        {
            throw new NotImplementedException();
        }
    }
}
