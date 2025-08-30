using HotChocolate.Configuration;
using HotChocolate.Data.Filters;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Raven.Filtering.Handlers;

public class RavenEnumNotInHandler : RavenComparableNotInHandler
{
    public RavenEnumNotInHandler(
        ITypeConverter typeConverter,
        InputParser inputParser)
        : base(typeConverter, inputParser)
    {
    }

    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeConfiguration typeConfiguration,
        IFilterFieldConfiguration fieldConfiguration)
    {
        return context.Type is IEnumOperationFilterInputType
            && fieldConfiguration is FilterOperationFieldConfiguration operationField
            && operationField.Id == Operation;
    }
}
