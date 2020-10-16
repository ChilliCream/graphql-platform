using System.Collections.Generic;
using HotChocolate.Data.Sorting;
using HotChocolate.Internal;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Sorting.Convention.Extensions.Handlers
{
    public class MongoDbSortVisitorContext : SortVisitorContext<SortDefinition<BsonDocument>>
    {
        public MongoDbSortVisitorContext(ISortInputType initialType)
            : base(initialType)
        {
        }

        public Stack<string> Path { get; } = new Stack<string>();
    }
}
