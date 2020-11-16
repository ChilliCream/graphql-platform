using System;
using System.Collections.Concurrent;
using HotChocolate.Execution;

namespace HotChocolate.Data.Sorting
{
    public class SchemaCache
        : SortVisitorTestBase,
          IDisposable
    {
        private readonly ConcurrentDictionary<(Type, Type, object), IRequestExecutor> _cache =
            new ConcurrentDictionary<(Type, Type, object), IRequestExecutor>();

        public IRequestExecutor CreateSchema<T, TType>(T?[] entites)
            where T : class
            where TType : SortInputType<T>
        {
            (Type, Type, T?[] entites) key = (typeof(T), typeof(TType), entites);
            return _cache.GetOrAdd(key, (k) => base.CreateSchema<T, TType>(entites));
        }

        public void Dispose()
        {
        }
    }
}
