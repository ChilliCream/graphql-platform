using HotChocolate.MongoDb.Data;

namespace HotChocolate.MongoDb.Execution
{
    public interface IMongoExecutable : IExecutable
    {
        IMongoExecutable WithFiltering(MongoDbFilterDefinition filters);

        IMongoExecutable WithSorting(MongoDbSortDefinition sorting);

        IMongoExecutable WithProjection(MongoDbProjectionDefinition projection);
    }
}
