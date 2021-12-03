using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public abstract class QueryableBooleanOperationHandler
    : QueryableOperationHandlerBase
{
    protected QueryableBooleanOperationHandler(InputParser inputParser) : base(inputParser)
    {
    }

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
