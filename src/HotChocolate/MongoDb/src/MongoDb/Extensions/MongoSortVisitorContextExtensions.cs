using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Sorting;
using HotChocolate.MongoDb.Sorting.Convention.Extensions.Handlers;
using MongoDB.Driver;

namespace HotChocolate.MongoDb.Data.Sorting
{
    internal static class MongoSortVisitorContextExtensions
    {
        public static bool TryCreateQuery<TEntityType>(
            this MongoDbSortVisitorContext context,
            [NotNullWhen(true)] out SortDefinition<TEntityType>? sortDefinition)
        {
            sortDefinition = null;

            if (context.Operations.Count == 0)
            {
                return false;
            }

            var sortDefinitionBuilder = Builders<TEntityType>.Sort;
            var sortDefinitions = new List<SortDefinition<TEntityType>>();

            foreach (var o in context.Operations)
            {
                if (o.Direction == DefaultSortOperations.Ascending)
                {
                    sortDefinitions.Add(sortDefinitionBuilder.Ascending(new ExpressionFieldDefinition<TEntityType>(o.LambdaExpression)));
                }
                else
                {
                    sortDefinitions.Add(sortDefinitionBuilder.Descending(new ExpressionFieldDefinition<TEntityType>(o.LambdaExpression)));
                }
            }

            sortDefinition = sortDefinitionBuilder.Combine(sortDefinitions);

            return true;
        }
    }
}
