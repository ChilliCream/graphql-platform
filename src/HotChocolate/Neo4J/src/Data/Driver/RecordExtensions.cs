using Neo4j.Driver;

namespace HotChocolate.Data.Neo4J
{
    public static class RecordExtensions
    {
        public static TReturn Map<TReturn>(
            this IRecord record)
        {
            return ValueMapper.MapValue<TReturn>(record[0]);
        }
    }
}
