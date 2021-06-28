using HotChocolate.Data.Filters;

namespace HotChocolate.Data.MongoDb.Filters
{
    /// <summary>
    /// This filter operation handler maps a Some operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class MongoDbListSomeOperationHandler : MongoDbListOperationHandlerBase
    {
        /// <inheritdoc />
        protected override int Operation => DefaultFilterOperations.Some;

        /// <inheritdoc />
        protected override MongoDbFilterDefinition HandleListOperation(
            MongoDbFilterVisitorContext context,
            IFilterField field,
            MongoDbFilterScope scope,
            string path)
        {
            return new MongoDbFilterOperation(
                path,
                new MongoDbFilterOperation("$elemMatch", CombineOperationsOfScope(scope)));
        }
    }
}
