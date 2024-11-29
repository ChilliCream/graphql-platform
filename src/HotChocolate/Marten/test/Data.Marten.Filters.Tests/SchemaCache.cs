using System.Collections.Concurrent;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;

namespace HotChocolate.Data;

public class SchemaCache : FilterVisitorTestBase
{
    private readonly ConcurrentDictionary<(Type, Type, object), Task<IRequestExecutor>> _cache = new();

    public async Task<IRequestExecutor> CreateSchemaAsync<T, TType>(
        T[] entities,
        bool withPaging = false,
        Action<ISchemaBuilder>? configure = null)
        where T : class
        where TType : FilterInputType<T>
    {
        (Type, Type, T[] entites) key = (typeof(T), typeof(TType), entities);
        return await _cache.GetOrAdd(
            key,
            async _ => await base.CreateSchemaAsync<T, TType>(
                entities,
                withPaging: withPaging,
                configure: configure));
    }
}
