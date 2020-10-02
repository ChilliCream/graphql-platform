using System.Collections.Generic;
using HotChocolate.Data.Filters;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoFilterScope
        : FilterScope<FilterDefinition<BsonDocument>>
    {
        public MongoFilterScope(MongoFilterVisitorContext context)
        {
            Context = context;
        }

        public MongoFilterVisitorContext Context { get; }

        public Stack<string> Path { get; } = new Stack<string>();
    }
}
