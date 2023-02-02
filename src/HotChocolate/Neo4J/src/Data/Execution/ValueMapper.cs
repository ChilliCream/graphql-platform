using System.Collections;
using Neo4j.Driver;
using ServiceStack;

namespace HotChocolate.Data.Neo4J;

internal static class ValueMapper
{
    public static T MapValue<T>(object cypherValue)
    {
        var targetType = typeof(T);

        if (typeof(IEnumerable).IsAssignableFrom(targetType))
        {
            if (cypherValue is not IEnumerable enumerable)
            {
                throw ThrowHelper
                    .ValueMapper_CypherValueIsNotAListAndCannotBeMapped(
                        targetType.UnderlyingSystemType);
            }

            if (targetType == typeof(string))
            {
                return enumerable.As<T>();
            }

            var elementType = targetType.GetGenericArguments()[0];
            var genericType = typeof(CollectionMapper<>).MakeGenericType(elementType);
            var collectionMapper = (ICollectionMapper)genericType.CreateInstance();

            return (T)collectionMapper.MapValues(enumerable, targetType);
        }

        return cypherValue switch
        {
            INode node => node.Properties.FromObjectDictionary<T>(),
            IRelationship relationship => relationship.Properties.FromObjectDictionary<T>(),
            IReadOnlyDictionary<string, object> map => map.FromObjectDictionary<T>(),
            IEnumerable =>
                throw ThrowHelper.ValueMapper_CypherValueIsAListAndCannotBeMapped(
                    targetType.UnderlyingSystemType),
            _ => cypherValue.As<T>()
        };
    }
}
