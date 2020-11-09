using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class MongoDbFilterCombinator
        : FilterOperationCombinator<MongoDbFilterVisitorContext, MongoDbFilterDefinition>
    {
        public override bool TryCombineOperations(
            MongoDbFilterVisitorContext context,
            Queue<MongoDbFilterDefinition> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out MongoDbFilterDefinition combined)
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

        private static MongoDbFilterDefinition CombineWithAnd(
            MongoDbFilterVisitorContext context,
            Queue<MongoDbFilterDefinition> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            return new AndFilterDefinition(operations.ToArray());
        }

        private static MongoDbFilterDefinition CombineWithOr(
            MongoDbFilterVisitorContext context,
            Queue<MongoDbFilterDefinition> operations)
        {
            if (operations.Count < 0)
            {
                throw new InvalidOperationException();
            }

            return new OrMongoDbFilterDefinition(operations.ToArray());
        }
    }
}
