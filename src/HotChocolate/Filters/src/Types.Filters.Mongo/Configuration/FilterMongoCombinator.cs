using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class FilterMongoCombinator
    {
        public static FilterDefinition<BsonDocument> CombineWithAnd(
            Queue<FilterDefinition<BsonDocument>> operations,
            IFilterVisitorContext<FilterDefinition<BsonDocument>> context)
        {
            if (operations.Count < 0 || !(context is MongoFilterVisitorContext ctx))
            {
                throw new InvalidOperationException();
            }

            return ctx.Builder.And(operations.ToArray());
        }

        public static FilterDefinition<BsonDocument> CombineWithOr(
            Queue<FilterDefinition<BsonDocument>> operations,
            IFilterVisitorContext<FilterDefinition<BsonDocument>> context)
        {
            if (operations.Count < 0 || !(context is MongoFilterVisitorContext ctx))
            {
                throw new InvalidOperationException();
            }

            return ctx.Builder.Or(operations.ToArray());
        }
    }
}
