using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Utilities;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public class MongoFilterVisitorContext
        : FilterVisitorContext<IMongoQuery>
    {
        public MongoFilterVisitorContext(
            IFilterInputType initialType,
            FilterVisitorDefinition<IMongoQuery> definition,
            ITypeConversion typeConverter)
            : base(initialType,
                  definition,
                  typeConverter)
        {

        }

        public override FilterScope<IMongoQuery> CreateScope() =>
             new MongoFilterScope();
    }
}
