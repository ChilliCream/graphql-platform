using HotChocolate.Configuration;

namespace HotChocolate.Data.Filters.Expressions
{
    public abstract class QueryableBooleanOperationHandler
        : QueryableOperationHandlerBase
    {
        protected abstract int Operation { get; }

        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is BooleanOperationFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == Operation;
        }
    }
}
