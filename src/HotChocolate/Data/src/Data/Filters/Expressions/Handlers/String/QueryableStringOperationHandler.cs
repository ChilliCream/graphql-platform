using HotChocolate.Configuration;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public abstract class QueryableStringOperationHandler : QueryableOperationHandlerBase
{
    protected QueryableStringOperationHandler(InputParser inputParser) : base(inputParser)
    {
    }

    protected abstract int Operation { get; }

    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeConfiguration typeConfiguration,
        IFilterFieldConfiguration fieldConfiguration)
    {
        return context.Type is StringOperationFilterInputType
            && fieldConfiguration is FilterOperationFieldConfiguration operationField
            && operationField.Id == Operation;
    }
}
