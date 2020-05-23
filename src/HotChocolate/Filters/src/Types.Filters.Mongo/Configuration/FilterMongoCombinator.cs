using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class FilterMongoCombinator
    {
        public static IMongoQuery CombineWithAnd(Queue<IMongoQuery> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            return Query.And(operations.ToArray());
        }

        public static IMongoQuery CombineWithOr(Queue<IMongoQuery> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            return Query.Or(operations.ToArray());
        }
    }
}
