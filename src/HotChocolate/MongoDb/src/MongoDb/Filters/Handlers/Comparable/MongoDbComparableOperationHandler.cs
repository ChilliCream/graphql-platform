using HotChocolate.Configuration;
using HotChocolate.Data.Filters;

namespace HotChocolate.MongoDb.Data.Filters
{
    public abstract class MongoDbComparableOperationHandler
        : MongoDbOperationHandlerBase
    {
        protected abstract int Operation { get; }

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            FilterInputTypeDefinition typeDefinition,
            FilterFieldDefinition fieldDefinition)
        {
            return context.Type is IComparableOperationFilterInput &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == Operation;
        }
    }
}
