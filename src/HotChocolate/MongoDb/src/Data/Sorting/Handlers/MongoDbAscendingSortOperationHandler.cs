using HotChocolate.Data.Sorting;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Sorting
{
    public class MongoDbAscendingSortOperationHandler : MongoDbSortOperationHandlerBase
    {
        public MongoDbAscendingSortOperationHandler()
            : base(DefaultSortOperations.Ascending, SortDirection.Ascending)
        {
        }
    }
}
