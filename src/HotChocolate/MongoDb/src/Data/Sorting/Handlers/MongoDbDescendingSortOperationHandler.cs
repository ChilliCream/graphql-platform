using HotChocolate.Data.Sorting;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Sorting
{
    public class MongoDbDescendingSortOperationHandler : MongoDbSortOperationHandlerBase
    {
        public MongoDbDescendingSortOperationHandler()
            : base(DefaultSortOperations.Descending, SortDirection.Descending)
        {
        }
    }
}
