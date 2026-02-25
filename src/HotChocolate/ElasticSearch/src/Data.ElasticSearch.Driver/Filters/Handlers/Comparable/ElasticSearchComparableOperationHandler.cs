using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters.Comparable;

public abstract class ElasticSearchComparableOperationHandler : ElasticSearchOperationHandlerBase
{
    /// <inheritdoc />
    protected ElasticSearchComparableOperationHandler(InputParser inputParser) : base(inputParser)
    {
    }

    /// <summary>
    /// Specifies the identifier of the operations that should be handled by this handler
    /// </summary>
    protected abstract int Operation { get; }

    /// <inheritdoc />
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeConfiguration typeDefinition,
        IFilterFieldConfiguration fieldDefinition)
    {
        return context.Type is IComparableOperationFilterInputType
            && fieldDefinition is FilterOperationFieldConfiguration operationField
            && operationField.Id == Operation;
    }
}
