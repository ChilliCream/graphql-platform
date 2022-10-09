using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Marten.Filtering;

public class MartenQueryableEnumNotInHandler
    : MartenQueryableComparableNotInHandler
{
    public MartenQueryableEnumNotInHandler(
        ITypeConverter typeConverter,
        InputParser inputParser)
        : base(typeConverter, inputParser)
    {
    }

    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
    {
        return context.Type is IEnumOperationFilterInputType &&
            fieldDefinition is FilterOperationFieldDefinition operationField &&
            operationField.Id == Operation;
    }
}
