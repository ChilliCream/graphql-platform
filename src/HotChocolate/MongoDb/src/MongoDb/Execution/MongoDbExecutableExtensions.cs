using HotChocolate.MongoDb.Execution;
using MongoDB.Driver;

namespace HotChocolate.MongoDb
{
    public static class MongoDbExecutableExtensions
    {
        public static MongoCollectionExecutable<T> AsExecutable<T>(
            this IMongoCollection<T> collection)
        {
            return new MongoCollectionExecutable<T>(collection);
        }
    }
}
