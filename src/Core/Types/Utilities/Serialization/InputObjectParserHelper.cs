using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Language;
using HotChocolate.Types;

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
                for (int i = 0; i < value.Fields.Count; i++)
                {
                    ObjectFieldNode fieldValue = value.Fields[i];
                    if (type.Fields.TryGetField(fieldValue.Name.Value, out InputField field))
                    {
                        dict[field.Name] = field.Type.ParseLiteral(fieldValue.Value);
                    }
                    else
                    {
                        throw new InputObjectSerializationException(
                            $"The field `{fieldValue.Name.Value}` does not exist on " +
                            $"the type `{type.Name}`.");
                    }
                }

                SetDefaultValues(type, dict);

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

            for (int i = 0; i < value.Fields.Count; i++)
            {
                ObjectFieldNode fieldValue = value.Fields[i];
                if (type.Fields.TryGetField(fieldValue.Name.Value, out InputField field))
                {
                    dict[field.Name] = field.Type.ParseLiteral(fieldValue.Value);
                }
                else
                {
                    throw new InputObjectSerializationException(
                        $"The field `{fieldValue.Name.Value}` does not exist on " +
                        $"the type `{type.Name}`.");
                }
            }

            SetDefaultValues(type, dict);

            return dict;
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
                foreach (KeyValuePair<string, object> fieldValue in value)
                {
                    if (type.Fields.TryGetField(fieldValue.Key, out InputField field))
                    {
                        dict[field.Name] = field.Type.Deserialize(fieldValue.Value);
                    }
                    else
                    {
                        throw new InputObjectSerializationException(
                            $"The field `{fieldValue.Key}` does not exist on " +
                            $"the type `{type.Name}`.");
                    }
                }

                SetDefaultValues(type, dict);

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

            foreach (KeyValuePair<string, object> fieldValue in value)
            {
                if (type.Fields.TryGetField(fieldValue.Key, out InputField field))
                {
                    dict[field.Name] = field.Type.Deserialize(fieldValue.Value);
                }
                else
                {
                    throw new InputObjectSerializationException(
                        $"The field `{fieldValue.Key}` does not exist on " +
                        $"the type `{type.Name}`.");
                }
            }

            SetDefaultValues(type, dict);

            return dict;
        }

        private static void SetDefaultValues(
            InputObjectType type,
            Dictionary<string, object> dict)
        {
            foreach (InputField field in type.Fields)
            {
                if (!field.IsOptional && !dict.ContainsKey(field.Name))
                {
                    dict[field.Name] = field.Type.ParseLiteral(field.DefaultValue);
                }
            }
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
