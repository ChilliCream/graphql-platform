using HotChocolate.Types.Filters.Conventions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HotChocolate.Types.Filters.Mongo
{
    public static class FilterMongoVisitorExtensions
    {
        public static IFilterVisitorDescriptor<FilterDefinition<BsonDocument>> UseDefault(
            this IFilterVisitorDescriptor<FilterDefinition<BsonDocument>> descriptor)
        {
            descriptor
                .Kind(FilterKind.Array)
                    .Enter(ArrayFieldHandler.Enter)
                    .Leave(ArrayFieldHandler.Leave)
                    .Operation(FilterOperationKind.ArrayAny)
                        .Handler(ArrayOperationHandler.ArrayAny).And()
                    .And()
                .Kind(FilterKind.Object)
                    .Enter(ObjectFieldHandler.Enter)
                    .And()
                .Kind(FilterKind.Boolean)
                    .Operation(FilterOperationKind.Equals)
                        .Handler(BooleanOperationHandlers.Equals).And()
                    .Operation(FilterOperationKind.NotEquals)
                        .Handler(BooleanOperationHandlers.NotEquals).And()
                        .And()
                .Kind(FilterKind.Comparable)
                    .Operation(FilterOperationKind.Equals)
                        .Handler(ComparableOperationHandlers.Equals).And()
                    .Operation(FilterOperationKind.NotEquals)
                        .Handler(ComparableOperationHandlers.NotEquals).And()
                    .Operation(FilterOperationKind.GreaterThan)
                        .Handler(ComparableOperationHandlers.GreaterThan).And()
                    .Operation(FilterOperationKind.NotGreaterThan)
                        .Handler(ComparableOperationHandlers.NotGreaterThan).And()
                    .Operation(FilterOperationKind.GreaterThanOrEquals)
                        .Handler(ComparableOperationHandlers.GreaterThanOrEquals).And()
                    .Operation(FilterOperationKind.NotGreaterThanOrEquals)
                        .Handler(ComparableOperationHandlers.NotGreaterThanOrEquals).And()
                    .Operation(FilterOperationKind.LowerThan)
                        .Handler(ComparableOperationHandlers.LowerThan).And()
                    .Operation(FilterOperationKind.NotLowerThan)
                        .Handler(ComparableOperationHandlers.NotLowerThan).And()
                    .Operation(FilterOperationKind.LowerThanOrEquals)
                        .Handler(ComparableOperationHandlers.LowerThanOrEquals).And()
                    .Operation(FilterOperationKind.NotLowerThanOrEquals)
                        .Handler(ComparableOperationHandlers.NotLowerThanOrEquals).And()
                    .Operation(FilterOperationKind.In)
                        .Handler(ComparableOperationHandlers.In).And()
                    .Operation(FilterOperationKind.NotIn)
                        .Handler(ComparableOperationHandlers.NotIn).And()
                        .And()
                .Kind(FilterKind.String)
                    .Operation(FilterOperationKind.Equals)
                        .Handler(StringOperationHandlers.Equals).And()
                    .Operation(FilterOperationKind.NotEquals)
                        .Handler(StringOperationHandlers.NotEquals).And()
                    .Operation(FilterOperationKind.Contains)
                        .Handler(StringOperationHandlers.Contains).And()
                    .Operation(FilterOperationKind.NotContains)
                        .Handler(StringOperationHandlers.NotContains).And()
                    .Operation(FilterOperationKind.StartsWith)
                        .Handler(StringOperationHandlers.StartsWith).And()
                    .Operation(FilterOperationKind.NotStartsWith)
                        .Handler(StringOperationHandlers.NotStartsWith).And()
                    .Operation(FilterOperationKind.EndsWith)
                        .Handler(StringOperationHandlers.EndsWith).And()
                    .Operation(FilterOperationKind.NotEndsWith)
                        .Handler(StringOperationHandlers.NotEndsWith).And()
                    .Operation(FilterOperationKind.In)
                        .Handler(StringOperationHandlers.In).And()
                    .Operation(FilterOperationKind.NotIn)
                        .Handler(StringOperationHandlers.NotIn).And()
                        .And()
                .Combinator(FilterCombinator.AND)
                        .Handler(FilterMongoCombinator.CombineWithAnd).And()
                .Combinator(FilterCombinator.OR)
                        .Handler(FilterMongoCombinator.CombineWithOr).And()
                .Middleware<FilterMongoVisitorMiddleware>();

            return descriptor;
        }
    }
}
