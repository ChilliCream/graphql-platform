using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbFilterCombinator
        : FilterOperationCombinator<MongoDbFilterVisitorContext, FilterDefinition<BsonDocument>>
    {
        public override bool TryCombineOperations(
            MongoDbFilterVisitorContext context,
            Queue<FilterDefinition<BsonDocument>> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out FilterDefinition<BsonDocument> combined)
        {
            if (operations.Count < 1)
            {
                throw new InvalidOperationException();
            }

            combined = combinator switch
            {
                FilterCombinator.And => CombineWithAnd(context, operations),
                FilterCombinator.Or => CombineWithOr(context, operations),
                _ => throw new InvalidOperationException()
            };

            return true;
        }

        private static FilterDefinition<BsonDocument> CombineWithAnd(
            MongoDbFilterVisitorContext context,
            Queue<FilterDefinition<BsonDocument>> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            return context.Builder.And(operations.ToArray());
        }

        private static FilterDefinition<BsonDocument> CombineWithOr(
            MongoDbFilterVisitorContext context,
            Queue<FilterDefinition<BsonDocument>> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            return context.Builder.Or(operations.ToArray());
        }
    }
}
