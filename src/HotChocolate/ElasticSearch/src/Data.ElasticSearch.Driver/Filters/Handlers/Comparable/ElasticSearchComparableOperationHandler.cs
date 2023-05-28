using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.ElasticSearch.Filters.Comparable;

public abstract class ElasticSearchComparableOperationHandler : ElasticSearchOperationHandlerBase
{
    /// <inheritdoc />
    public ElasticSearchComparableOperationHandler(InputParser inputParser) : base(inputParser)
    {
    }

    /// <summary>
    /// Specifies the identifier of the operations that should be handled by this handler
    /// </summary>
    protected abstract int Operation { get; }

    /// <inheritdoc />
    public override bool CanHandle(ITypeCompletionContext context, IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
    {
        return context.Type is IComparableOperationFilterInputType &&
               fieldDefinition is FilterOperationFieldDefinition operationField &&
               operationField.Id == Operation;
    }
}
