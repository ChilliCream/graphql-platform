using HotChocolate.Data.Neo4J.Language;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.Neo4J.Sorting
{
    public class Neo4JAscendingSortOperationHandler
        : Neo4JSortOperationHandlerBase
    {
        public Neo4JAscendingSortOperationHandler()
            : base(DefaultSortOperations.Ascending, SortDirection.Ascending)
        {
        }
    }
}
