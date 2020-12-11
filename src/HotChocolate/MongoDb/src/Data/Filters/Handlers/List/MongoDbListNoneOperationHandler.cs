using HotChocolate.Data.Filters;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters
{
    /// <summary>
    /// This filter operation handler maps a All operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class MongoDbListNoneOperationHandler : MongoDbListOperationHandlerBase
    {
        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.None;

        /// <inheritdoc />
        protected override MongoDbFilterDefinition HandleListOperation(
            MongoDbFilterVisitorContext context,
            IFilterField field,
            MongoDbFilterScope scope,
            string path)
        {
            return new MongoDbFilterOperation(
                path,
                new NotMongoDbFilterDefinition(
                    new MongoDbFilterOperation("$elemMatch", CombineOperationsOfScope(scope))));
        }
    }
}
