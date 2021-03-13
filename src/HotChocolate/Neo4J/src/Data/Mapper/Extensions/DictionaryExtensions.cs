using System.Collections.Generic;
using System.Reflection;

#nullable enable

namespace HotChocolate.Data.Neo4J
{
    public static class DictionaryExtensions
    {
        public static IDictionary<string, object> WithEntity<T>(
            this IDictionary<string, object> dictionary,
            string key, T entity)
            where  T : class
        {
            dictionary.WithMap(entity.ToParameterMap(key));

            return dictionary;
        }

        public static IDictionary<string, object> WithEntities<T>(
            this IDictionary<string, object> dictionary,
            string key, IEnumerable<T> entities) where  T : class
        {
            dictionary.WithMaps(entities.ToParameterMaps(key));

            return dictionary;
        }

        public static IDictionary<string, object> WithMap(
            this IDictionary<string, object> dictionary,
            KeyValuePair<string, IReadOnlyDictionary<string, object>> kvp)
        {
            dictionary.Add(kvp.Key, kvp.Value);

            return dictionary;
        }

        public static IDictionary<string, object> WithMaps(
            this IDictionary<string, object> dictionary,
            KeyValuePair<string, IEnumerable<IReadOnlyDictionary<string, object>>> kvp)
        {
            dictionary.Add(kvp.Key, kvp.Value);

            return dictionary;
        }

        public static IDictionary<string, object> WithParams(
            this IDictionary<string, object> dictionary,
            object? parameters)
        {
            if (parameters == null) return dictionary;

            foreach (var propertyInfo in (parameters.GetType().GetTypeInfo().DeclaredProperties))
            {
                var key = propertyInfo.Name;
                var value = propertyInfo.GetValue(parameters);
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        public static IDictionary<string, object> WithValue(this IDictionary<string, object> dictionary, string key, object value)
        {
            dictionary.Add(key, CypherQueryParameters.ValueConvert((key, value)));

            return dictionary;
        }
    }
}
