using HotChocolate.Data.Sorting;
using MongoDB.Driver;

namespace HotChocolate.Data.MongoDb.Sorting;

public class MongoDbAscendingSortOperationHandler()
    : MongoDbSortOperationHandlerBase(DefaultSortOperations.Ascending, SortDirection.Ascending);
