using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbNotEqualsOperationHandler
        : MongoDbOperationHandlerBase
    {
        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition)
        {
            return fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id is DefaultOperations.NotEquals;
        }

        public override FilterDefinition<BsonDocument> HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            var doc = new BsonDocument(
                "$ne",
                BsonValue.Create(parsedValue));

            return new BsonDocument(
                context.GetMongoFilterScope().GetPath(),
                doc);
        }
    }
}
