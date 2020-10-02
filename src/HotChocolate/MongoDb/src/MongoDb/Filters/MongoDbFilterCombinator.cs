using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class FilterMongoCombinator
        : FilterOperationCombinator<MongoFilterVisitorContext, FilterDefinition<BsonDocument>>
    {
        public override bool TryCombineOperations(
            MongoFilterVisitorContext context,
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
                _ => throw new InvalidOperationException(),
            };

            return true;
        }

        private static FilterDefinition<BsonDocument> CombineWithAnd(
            MongoFilterVisitorContext context,
            Queue<FilterDefinition<BsonDocument>> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            return context.Builder.And(operations.ToArray());
        }

        private static FilterDefinition<BsonDocument> CombineWithOr(
            MongoFilterVisitorContext context,
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
