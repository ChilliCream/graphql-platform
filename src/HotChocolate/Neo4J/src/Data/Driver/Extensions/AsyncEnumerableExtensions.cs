using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public static class AsyncEnumerableExtensions
    {
        public static IAsyncEnumerable<TReturn> Map<TReturn>(
            this IAsyncEnumerable<IRecord> records)
        {
            return records.Select(record => record.Map<TReturn>());
        }
    }
}
