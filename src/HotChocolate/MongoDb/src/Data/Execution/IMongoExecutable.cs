using HotChocolate.Data.MongoDb;

namespace HotChocolate.Data.MongoDb.Execution
{
    public interface IMongoExecutable : IExecutable
    {
        IMongoExecutable WithFiltering(MongoDbFilterDefinition filters);

        IMongoExecutable WithSorting(MongoDbSortDefinition sorting);

        IMongoExecutable WithProjection(MongoDbProjectionDefinition projection);
    }
}
