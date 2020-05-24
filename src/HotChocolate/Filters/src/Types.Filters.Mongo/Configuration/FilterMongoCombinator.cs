using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class FilterMongoCombinator
    {
        public static FilterDefinition<BsonDocument> CombineWithAnd(
            Queue<FilterDefinition<BsonDocument>> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            return Query.And(operations.ToArray());
        }

        public static FilterDefinition<BsonDocument> CombineWithOr(
            Queue<FilterDefinition<BsonDocument>> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            return Query.Or(operations.ToArray());
        }
    }
}
