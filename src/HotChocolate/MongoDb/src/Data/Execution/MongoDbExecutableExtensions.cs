using HotChocolate.Data.MongoDb.Execution;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb
{
    public static class MongoDbExecutableExtensions
    {
        public static MongoCollectionExecutable<T> AsExecutable<T>(
            this IMongoCollection<T> collection)
        {
            return new MongoCollectionExecutable<T>(collection);
        }

        public static MongoAggregateFluentExecutable<T> AsExecutable<T>(
            this IAggregateFluent<T> collection)
        {
            return new MongoAggregateFluentExecutable<T>(collection);
        }

        public static MongoFindFluentExecutable<T> AsExecutable<T>(
            this IFindFluent<T, T> collection)
        {
            return new MongoFindFluentExecutable<T>(collection);
        }
    }
}
