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
        IFilterInputTypeConfiguration typeConfiguration,
        IFilterFieldConfiguration fieldConfiguration)
    {
        return context.Type is BooleanOperationFilterInputType
            && fieldConfiguration is FilterOperationFieldConfiguration operationField
            && operationField.Id == Operation;
    }
}
