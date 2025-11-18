using HotChocolate.Data.Sorting;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Sorting;

public class MongoDbDescendingSortOperationHandler()
    : MongoDbSortOperationHandlerBase(DefaultSortOperations.Descending, SortDirection.Descending)
{
    public static MongoDbDescendingSortOperationHandler Create(SortProviderContext context) => new();
}
