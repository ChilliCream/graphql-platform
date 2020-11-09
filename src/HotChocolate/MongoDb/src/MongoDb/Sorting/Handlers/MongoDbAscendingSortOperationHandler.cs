using HotChocolate.Data.Sorting;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Sorting.Handlers
{
    public class MongoDbAscendingSortOperationHandler : MongoDbSortOperationHandlerBase
    {
        public MongoDbAscendingSortOperationHandler()
            : base(DefaultSortOperations.Ascending, SortDirection.Ascending)
        {
        }
    }
}
