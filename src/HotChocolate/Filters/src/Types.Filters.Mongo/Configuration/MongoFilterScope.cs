using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
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
