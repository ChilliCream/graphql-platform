using HotChocolate.Configuration;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Sorting;

public class MatchAnyQueryableOperationHandler
    : SortOperationHandler<QueryableSortContext, QueryableSortOperation>
{
    public override bool CanHandle(
        ITypeCompletionContext context,
        EnumTypeConfiguration typeDefinition,
        SortEnumValueConfiguration valueConfiguration) => true;
}
