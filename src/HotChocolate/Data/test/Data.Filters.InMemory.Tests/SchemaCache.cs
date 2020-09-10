using System;
using System.Collections.Concurrent;
using HotChocolate.Execution;

namespace HotChocolate.Data.Filters
{
    public class SchemaCache : FilterVisitorTestBase, IDisposable
    {
        private readonly ConcurrentDictionary<(Type, Type, object), IRequestExecutor> _cache =
            new ConcurrentDictionary<(Type, Type, object), IRequestExecutor>();

        public SchemaCache()
            : base()
        {

        }

        public IRequestExecutor CreateSchema<T, TType>(T[] entites)
            where T : class
            where TType : FilterInputType<T>
        {
            (Type, Type, T[] entites) key = (typeof(T), typeof(TType), entites);
            return _cache.GetOrAdd(key, (k) => base.CreateSchema<T, TType>(entites));
        }

        public void Dispose()
        {
        }
    }
}
