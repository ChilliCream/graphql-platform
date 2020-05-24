using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public class MongoFilterVisitorContext
        : FilterVisitorContext<FilterDefinition<BsonDocument>>
    {
        public MongoFilterVisitorContext(
            IFilterInputType initialType,
            FilterVisitorDefinition<FilterDefinition<BsonDocument>> definition,
            ITypeConversion typeConverter)
            : base(initialType, definition, typeConverter)
        {
        }

        public FilterDefinitionBuilder<BsonDocument> Builder { get; } =
            new FilterDefinitionBuilder<BsonDocument>();

        public override FilterScope<FilterDefinition<BsonDocument>> CreateScope() =>
             new MongoFilterScope(this);
    }
}
