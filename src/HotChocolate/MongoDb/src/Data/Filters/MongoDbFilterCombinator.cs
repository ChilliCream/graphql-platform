using System.Diagnostics.CodeAnalysis;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data.MongoDb.Filters;

/// <inheritdoc />
public class MongoDbFilterCombinator
    : FilterOperationCombinator<MongoDbFilterVisitorContext, MongoDbFilterDefinition>
{
    /// <inheritdoc />
    public override bool TryCombineOperations(
        MongoDbFilterVisitorContext context,
        Queue<MongoDbFilterDefinition> operations,
        FilterCombinator combinator,
        [NotNullWhen(true)] out MongoDbFilterDefinition? combined)
    {
        if (operations.Count == 0)
        {
            combined = default;
            return false;
        }

        combined = combinator switch
        {
            FilterCombinator.And => CombineWithAnd(operations),
            FilterCombinator.Or => CombineWithOr(operations),
            _ => throw ThrowHelper
                .Filtering_MongoDbCombinator_InvalidCombinator(this, combinator),
        };

        return true;
    }

    private static MongoDbFilterDefinition CombineWithAnd(
        Queue<MongoDbFilterDefinition> operations)
    {
        if (operations.Count == 0)
        {
            throw new InvalidOperationException();
        }

        return new AndFilterDefinition(operations.ToArray());
    }

    private static MongoDbFilterDefinition CombineWithOr(
        Queue<MongoDbFilterDefinition> operations)
    {
        if (operations.Count == 0)
        {
            throw new InvalidOperationException();
        }

        return new OrMongoDbFilterDefinition(operations.ToArray());
    }
}
