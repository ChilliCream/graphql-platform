using HotChocolate.Data.Sorting;

namespace HotChocolate.MongoDb.Sorting.Handlers
{
    public class MongoDbAscendingSortOperationHandler : MongoDbSortOperationHandlerBase
    {
        public MongoDbAscendingSortOperationHandler()
            : base(DefaultSortOperations.Ascending, 1)
        {
        }
    }
}
