using System.Collections.Generic;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbFilterVisitorContext
        : FilterVisitorContext<MongoDbFilterDefinition>
    {
        public MongoDbFilterVisitorContext(IFilterInputType initialType)
            : base(initialType)
        {
            RuntimeTypes = new Stack<IExtendedType>();
            RuntimeTypes.Push(initialType.EntityType);
        }

        public FilterDefinitionBuilder<BsonDocument> Builder { get; } =
            new FilterDefinitionBuilder<BsonDocument>();

        public Stack<IExtendedType> RuntimeTypes { get; }

        public override FilterScope<MongoDbFilterDefinition> CreateScope() =>
            new MongoDbFilterScope(this);
    }
}
