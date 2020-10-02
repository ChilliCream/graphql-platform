using System.Collections.Generic;
using HotChocolate.Data.Filters;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbFilterScope
        : FilterScope<FilterDefinition<BsonDocument>>
    {
        public MongoDbFilterScope(MongoDbFilterVisitorContext context)
        {
            Context = context;
        }

        public MongoDbFilterVisitorContext Context { get; }

        public Stack<string> Path { get; } = new Stack<string>();
    }
}
