using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Data.Sorting.Expressions;

namespace HotChocolate.Data.Sorting
{
    public class MatchAnyQueryableFieldHandler
        : SortFieldHandler<QueryableSortContext, QueryableSortOperation>
    {
        public override bool CanHandle(
            ITypeDiscoveryContext context,
            SortInputTypeDefinition typeDefinition,
            SortFieldDefinition fieldDefinition) => true;
    }
}
