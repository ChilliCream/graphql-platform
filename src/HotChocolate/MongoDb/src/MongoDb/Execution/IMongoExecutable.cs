using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Execution
{
    public interface IMongoExecutable : IExecutable
    {
        IMongoExecutable WithFiltering(FilterDefinition<BsonDocument> filters);

        IMongoExecutable WithSorting(SortDefinition<BsonDocument> sorting);
    }
}
