using System;
using System.Collections;
using System.Collections.Generic;
using Neo4j.Driver;
using ServiceStack;

namespace HotChocolate.Data.Neo4J
{
    public static class ValueMapper
    {
        public static T MapValue<T>(object cypherValue)
        {
            Type targetType = typeof(T);

            if (typeof(IEnumerable).IsAssignableFrom(targetType))
            {
                if (!(cypherValue is IEnumerable enumerable))
                    throw new InvalidOperationException($"The cypher value is not a list and cannot be mapped to target type: {targetType.UnderlyingSystemType}");

                if (targetType == typeof(string))
                {
                    return enumerable.As<T>();
                }

                Type? elementType = targetType.GetGenericArguments()[0];
                Type genericType = typeof(CollectionMapper<>).MakeGenericType(elementType);
                var collectionMapper = (ICollectionMapper)genericType.CreateInstance();

                return (T)collectionMapper.MapValues(enumerable, targetType);
            }

            switch (cypherValue)
            {
                case INode node:
                {
                    T entity = node.Properties.FromObjectDictionary<T>();
                    EntityPropertyAccessor.SetNodeId(entity, node.Id);

                    return entity;
                }
                case IRelationship relationship:
                {
                    T entity = relationship.Properties.FromObjectDictionary<T>();

                    return entity;
                }
                case IReadOnlyDictionary<string, object> map:
                    return map.FromObjectDictionary<T>();
                case IEnumerable:
                    throw new InvalidOperationException($"The cypher value is a list and cannot be mapped to target type: {targetType.UnderlyingSystemType}");
                default:
                    return cypherValue.As<T>();
            }
        }
    }
}
