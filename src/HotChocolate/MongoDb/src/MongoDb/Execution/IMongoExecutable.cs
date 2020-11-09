using HotChocolate.MongoDb.Data.Filters;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Execution
{
    public interface IMongoExecutable : IExecutable
    {
        IMongoExecutable WithFiltering(MongoDbFilterDefinition filters);

        IMongoExecutable WithSorting(SortDefinition<BsonDocument> sorting);
    }
}
