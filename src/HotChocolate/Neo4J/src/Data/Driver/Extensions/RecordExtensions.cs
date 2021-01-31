using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public static class RecordExtensions
    {
        public static IEnumerable<TReturn> Map<TReturn>(
            this IEnumerable<IRecord> records)
        {
            return records.Select(record => record.Map<TReturn>());
        }

        public static TReturn Map<TReturn>(
            this IRecord record)
        {
            return ValueMapper.MapValue<TReturn>(record[0]);
        }
    }
}
