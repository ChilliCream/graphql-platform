using HotChocolate.Configuration;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class MatchAnyQueryableOperationHandler
        : SortOperationHandler<QueryableSortContext, QueryableSortOperation>
    {
        public override bool CanHandle(
            ITypeCompletionContext context,
            EnumTypeDefinition typeDefinition,
            SortEnumValueDefinition valueDefinition) => true;
    }
}
