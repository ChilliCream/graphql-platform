using MongoDB.Driver;
using System.Linq;

namespace HotChocolate.MongoDb.Execution
{
    public static class AggregateFluentExtensions
    {
        public static IAggregateFluentExecutable<T> AsExecutable<T>(this IAggregateFluent<T> aggregateFluent)
        {
            return new AggregateFluentExecutable<T>(aggregateFluent);
        }

        public static IAggregateFluentExecutable<T> AsExecutable<T>(this IMongoCollection<T> collection)
        {
            return new AggregateFluentExecutable<T>(collection.Aggregate());
        }
    }
}
