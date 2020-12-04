using HotChocolate.Configuration;
using HotChocolate.Data.Filters;

namespace HotChocolate.MongoDb.Data.Filters
{
    public abstract class MongoDbComparableOperationHandler
        : MongoDbOperationHandlerBase
    {
        protected abstract int Operation { get; }

        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is IComparableOperationFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == Operation;
        }
    }
}
