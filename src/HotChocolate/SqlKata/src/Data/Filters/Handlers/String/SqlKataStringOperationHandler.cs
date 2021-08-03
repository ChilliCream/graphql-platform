using HotChocolate.Configuration;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data.SqlKata.Filters
{
    public abstract class SqlKataStringOperationHandler
        : SqlKataOperationHandlerBase
    {
        protected abstract int Operation { get; }

        public override bool CanHandle(
            ITypeCompletionContext context,
            IFilterInputTypeDefinition typeDefinition,
            IFilterFieldDefinition fieldDefinition)
        {
            return context.Type is StringOperationFilterInputType &&
                fieldDefinition is FilterOperationFieldDefinition operationField &&
                operationField.Id == Operation;
        }
    }
}
