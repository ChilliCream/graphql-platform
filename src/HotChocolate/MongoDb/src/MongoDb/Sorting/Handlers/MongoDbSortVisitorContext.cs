using System.Collections.Generic;
using HotChocolate.Data.Sorting;
using HotChocolate.Internal;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Sorting.Convention.Extensions.Handlers
{
    public class MongoDbSortVisitorContext : SortVisitorContext<SortDefinition>
    {
        public MongoDbSortVisitorContext(ISortInputType initialType)
            : base(initialType)
        {
        }
    }
}
