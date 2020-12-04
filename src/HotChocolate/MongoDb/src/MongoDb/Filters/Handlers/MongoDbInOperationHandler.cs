using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbInOperationHandler
        : MongoDbOperationHandlerBase
    {
        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id is DefaultFilterOperations.In;
        }

        public override MongoDbFilterDefinition HandleOperation(
            MongoDbFilterVisitorContext context,
            IFilterOperationField field,
            IValueNode value,
            object? parsedValue)
        {
            var doc = new MongoDbFilterOperation("$in", parsedValue);

            return new MongoDbFilterOperation(context.GetMongoFilterScope().GetPath(), doc);
        }
    }
}
