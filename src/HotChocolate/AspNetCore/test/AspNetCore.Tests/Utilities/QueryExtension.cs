using System;
using HotChocolate.Types;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Utilities
{
    [ExtendObjectType("Query")]
    public class QueryExtension
    {
        private readonly DateTime _time = DateTime.UtcNow;

        public long Time() => _time.Ticks;

        public bool Evict([Service]IRequestExecutorResolver executorResolver, ISchema schema)
        {
            executorResolver.EvictRequestExecutor(schema.Name);
            return true;
        }
    }
}
