using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters
{
    /// <summary>
    /// This filter operation handler maps a NotIn operation field to a
    /// <see cref="FilterDefinition{TDocument}"/>
    /// </summary>
    public class MongoDbNotInOperationHandler
        : MongoDbOperationHandlerBase
    {
        /// <inheritdoc />
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id is DefaultFilterOperations.NotIn;
        }

        /// <inheritdoc />
        public override MongoDbFilterDefinition HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            var doc = new MongoDbFilterOperation("$nin", parsedValue);

            return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
        }
    }
}
