using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Language;
using HotChocolate.Types;
using System;

namespace HotChocolate.Utilities.Serialization
{
    internal static class InputObjectParserHelper
    {
        private static readonly DefaultObjectPool<Dictionary<string, object>> _dictionaryPool =
            new DefaultObjectPool<Dictionary<string, object>>(
                new DictionaryPoolPolicy(),
                32);

        public static object Parse(
            InputObjectType type,
            ObjectValueNode value,
            InputObjectFactory factory,
            ITypeConversion converter)
        {
            Dictionary<string, object> dict = _dictionaryPool.Get();

            try
            {
                Parse(type, value, dict, converter);
                SetDefaultValues(type, dict, converter);
                return factory(dict, converter);
            }
            finally
            {
                _dictionaryPool.Return(dict);
            }
        }

        public static object Parse(
            InputObjectType type,
            ObjectValueNode value,
            ITypeConversion converter)
        {
            var dict = new Dictionary<string, object>();

            Parse(type, value, dict, converter);
            SetDefaultValues(type, dict, converter);

            return dict;
        }

        private static void Parse(
            InputObjectType type,
            ObjectValueNode source,
            IDictionary<string, object> target,
            ITypeConversion converter)
        {
            for (int i = 0; i < source.Fields.Count; i++)
            {
                ObjectFieldNode fieldValue = source.Fields[i];
                if (type.Fields.TryGetField(fieldValue.Name.Value, out InputField field))
                {
                    object value = field.Type.ParseLiteral(fieldValue.Value);
                    value = field.Serializer is null ? value : field.Serializer.Deserialize(value);
                    target[field.Name] = ConvertValue(field, converter, value);
                }
                else
                {
                    throw new InputObjectSerializationException(
                        $"The field `{fieldValue.Name.Value}` does not exist on " +
                        $"the type `{type.Name}`.");
                }
            }
        }

        public static object Deserialize(
            InputObjectType type,
            IReadOnlyDictionary<string, object> value,
            InputObjectFactory factory,
            ITypeConversion converter)
        {
            Dictionary<string, object> dict = _dictionaryPool.Get();

            try
            {
                Deserialize(type, value, dict, converter);
                SetDefaultValues(type, dict, converter);
                return factory(dict, converter);
            }
            finally
            {
                _dictionaryPool.Return(dict);
            }
        }

        public static Dictionary<string, object> Deserialize(
            InputObjectType type,
            IReadOnlyDictionary<string, object> value,
            ITypeConversion converter)
        {
            var dict = new Dictionary<string, object>();

            Deserialize(type, value, dict, converter);
            SetDefaultValues(type, dict, converter);

            return dict;
        }

        private static void Deserialize(
            InputObjectType type,
            IReadOnlyDictionary<string, object> source,
            IDictionary<string, object> target,
            ITypeConversion converter)
        {
            foreach (KeyValuePair<string, object> fieldValue in source)
            {
                if (type.Fields.TryGetField(fieldValue.Key, out InputField field))
                {
                    object value = field.Type.Deserialize(fieldValue.Value);
                    value = field.Serializer is null ? value : field.Serializer.Deserialize(value);
                    target[field.Name] = ConvertValue(field, converter, value);
                }
                else
                {
                    throw new InputObjectSerializationException(
                        $"The field `{fieldValue.Key}` does not exist on " +
                        $"the type `{type.Name}`.");
                }
            }
        }

        private static void SetDefaultValues(
            InputObjectType type,
            IDictionary<string, object> dict,
            ITypeConversion converter)
        {
            foreach (InputField field in type.Fields)
            {
                if (!field.IsOptional && !dict.ContainsKey(field.Name))
                {
                    object value = field.Type.ParseLiteral(field.DefaultValue);
                    dict[field.Name] = ConvertValue(field, converter, value);
                }
            }
        }

        private static object ConvertValue(
            InputField field,
            ITypeConversion converter,
            object value)
        {
            if (value is { }
                && field.ClrType != typeof(object))
            {
                Type type = field.ClrType;

                if (type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(Optional<>))
                {
                    type = type.GetGenericArguments()[0];
                }

                value = converter.Convert(value.GetType(), type, value);
            }
            return value;
        }

        private class DictionaryPoolPolicy
            : PooledObjectPolicy<Dictionary<string, object>>
        {
            public override Dictionary<string, object> Create()
            {
                return new Dictionary<string, object>();
            }

            public override bool Return(Dictionary<string, object> obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
