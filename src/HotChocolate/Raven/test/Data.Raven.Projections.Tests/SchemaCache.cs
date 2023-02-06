using System.Collections.Concurrent;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

namespace HotChocolate.Data.Raven;

public class SchemaCache
    : ProjectionVisitorTestBase,
      IDisposable
{
    private readonly ConcurrentDictionary<(Type, object, bool, bool), IRequestExecutor> _cache =
        new();

    public IRequestExecutor CreateSchema<T>(
        T[] entities,
        bool usePaging = false,
        bool useOffsetPaging = false,
        INamedType? objectType = null,
        Action<IRequestExecutorBuilder>? configure = null,
        Type? schemaType = null)
        where T : class
    {
        var key = (typeof(T), entities, usePaging, useOffsetPaging);

        return _cache.GetOrAdd(
            key,
            _ => base.CreateSchema(
                entities,
                usePaging: usePaging,
                useOffsetPaging: useOffsetPaging,
                objectType: objectType,
                configure: configure,
                schemaType: schemaType));
    }

    public void Dispose()
    {
    }
}
