using System.Collections.Generic;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public class MongoFilterScope
        : FilterScope<IMongoQuery>
    {
        public Stack<string> Path { get; } = new Stack<string>();
    }
}
