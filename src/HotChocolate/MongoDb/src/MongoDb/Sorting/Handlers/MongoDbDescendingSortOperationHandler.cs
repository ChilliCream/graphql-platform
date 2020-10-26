using HotChocolate.Data.Sorting;

namespace HotChocolate.MongoDb.Sorting.Handlers
{
    public class MongoDbDescendingSortOperationHandler : MongoDbSortOperationHandlerBase
    {
        public MongoDbDescendingSortOperationHandler()
            : base(DefaultSortOperations.Descending, DefaultSortOperations.Descending)
        {
        }
    }
}
