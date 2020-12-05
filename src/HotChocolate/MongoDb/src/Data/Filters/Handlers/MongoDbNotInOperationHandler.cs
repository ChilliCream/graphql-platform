using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Filters
{
    public class MongoDbNotInOperationHandler
        : MongoDbOperationHandlerBase
    {
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id is DefaultFilterOperations.NotIn;
        }

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
