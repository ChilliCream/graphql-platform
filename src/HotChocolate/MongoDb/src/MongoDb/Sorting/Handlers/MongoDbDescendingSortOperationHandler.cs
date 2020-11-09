using HotChocolate.Data.Sorting;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Sorting.Handlers
{
    public class MongoDbDescendingSortOperationHandler : MongoDbSortOperationHandlerBase
    {
        public MongoDbDescendingSortOperationHandler()
            : base(DefaultSortOperations.Descending, SortDirection.Descending)
        {
        }
    }
}
