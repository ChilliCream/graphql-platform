using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.Neo4J.Sorting
{
    public class Neo4JDescendingSortOperationHandler : Neo4JSortOperationHandlerBase
    {
        public Neo4JDescendingSortOperationHandler()
            : base(DefaultSortOperations.Descending, SortDirection.Descending)
        {
        }
    }
}
