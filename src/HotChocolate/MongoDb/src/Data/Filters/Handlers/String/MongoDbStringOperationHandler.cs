using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data.MongoDb.Filters;

public abstract class MongoDbStringOperationHandler
    : MongoDbOperationHandlerBase
{
    protected MongoDbStringOperationHandler(InputParser inputParser) : base(inputParser)
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
