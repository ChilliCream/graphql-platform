using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Raven.Filtering;

public class RavenQueryableEnumInHandler
    : RavenQueryableComparableInHandler
{
    public RavenQueryableEnumInHandler(
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
