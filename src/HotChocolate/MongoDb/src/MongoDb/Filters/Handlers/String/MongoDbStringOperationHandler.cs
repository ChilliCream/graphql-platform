using HotChocolate.Configuration;
using HotChocolate.Data.Filters;

namespace HotChocolate.MongoDb.Data.Filters
{
    public abstract class MongoDbStringOperationHandler
        : MongoDbOperationHandlerBase
    {
        protected abstract int Operation { get; }

        public override bool CanHandle(
            ITypeDiscoveryContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is StringOperationFilterInput &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == Operation;
        }
    }
}
