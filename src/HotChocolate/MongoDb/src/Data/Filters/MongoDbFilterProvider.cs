using System.Text.RegularExpressions;
using HotChocolate.Data.Filters;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using static HotChocolate.Data.MongoDb.MongoDbContextData;

namespace HotChocolate.Data.MongoDb.Filters;

/// <summary>
/// A <see cref="FilterProvider{TContext}"/> translates an incoming query to a
/// <see cref="FilterDefinition{T}"/>
/// </summary>
public class MongoDbFilterProvider : FilterProvider<MongoDbFilterVisitorContext>
{
    /// <inheritdoc />
    public MongoDbFilterProvider()
    {
    }

    /// <inheritdoc />
    public MongoDbFilterProvider(
        Action<IFilterProviderDescriptor<MongoDbFilterVisitorContext>> configure)
        : base(configure)
    {
    }

    /// <summary>
    /// The visitor that is used to traverse the incoming selection set an execute handlers
    /// </summary>
    protected virtual FilterVisitor<MongoDbFilterVisitorContext, MongoDbFilterDefinition> Visitor { get; } =
        new(new MongoDbFilterCombinator());

    public override IQueryBuilder CreateBuilder<TEntityType>(string argumentName)
        => new MongoDbQueryBuilder<TEntityType>(CreateFilterDefinition(argumentName));

    private Func<IMiddlewareContext, MongoDbFilterDefinition?> CreateFilterDefinition(string argumentName)
        => context =>
        {
            // next we get the filter argument.
            var argument = context.Selection.Field.Arguments[argumentName];
            var filter = context.ArgumentLiteral<IValueNode>(argumentName);

            // if no filter is defined we can stop here and yield back control.
            var skipFiltering = context.GetLocalStateOrDefault<bool>(SkipFilteringKey);

            // ensure filtering is only applied once
            context.SetLocalState(SkipFilteringKey, true);

            if (filter.IsNull() || skipFiltering || argument.Type is not IFilterInputType filterInput)
            {
                return null;
            }

            var visitorContext = new MongoDbFilterVisitorContext(filterInput);

            Visitor.Visit(filter, visitorContext);

            if (visitorContext.Errors.Count == 0)
            {
                return visitorContext.CreateQuery();
            }

            throw new GraphQLException(
                visitorContext.Errors.Select(e => e.WithPath(context.Path)).ToArray());
        };

    private sealed class MongoDbQueryBuilder<TEntityType>(
        Func<IMiddlewareContext, MongoDbFilterDefinition?> createFilterDef)
        : IQueryBuilder
    {
        public void Prepare(IMiddlewareContext context)
        {
            var filterDef = createFilterDef(context);
            context.SetLocalState(FilterDefinitionKey, filterDef);
        }

        public void Apply(IMiddlewareContext context)
        {
            var filterDef = context.GetLocalStateOrDefault<MongoDbFilterDefinition>(FilterDefinitionKey);

            if (filterDef is null)
            {
                return;
            }

            if (context.Result is IMongoDbExecutable executable)
            {
                context.Result = executable.WithFiltering(filterDef);
                return;
            }

            if (TryApplyInMemoryFiltering<TEntityType>(context.Result, filterDef, out var filtered))
            {
                context.Result = filtered;
            }
        }
    }

    private static bool TryApplyInMemoryFiltering<TEntityType>(
        object? source,
        MongoDbFilterDefinition filterDef,
        out object? filtered)
    {
        filtered = null;

        IEnumerable<TEntityType>? enumerable = source switch
        {
            IQueryableExecutable<TEntityType> q => q.Source,
            IQueryable<TEntityType> q => q,
            IEnumerable<TEntityType> q => q,
            _ => null
        };

        if (enumerable is null)
        {
            return false;
        }

        try
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<TEntityType>();
            var renderedFilter = filterDef.ToFilterDefinition<TEntityType>().Render(
                new RenderArgs<TEntityType>(serializer, BsonSerializer.SerializerRegistry));

            List<TEntityType> result = [];

            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    continue;
                }

                var document = item.ToBsonDocument();

                if (!TryMatchesDocument(document, renderedFilter, out var isMatch))
                {
                    return false;
                }

                if (isMatch)
                {
                    result.Add(item);
                }
            }

            filtered = source switch
            {
                IQueryableExecutable<TEntityType> q => q.WithSource(result.AsQueryable()),
                IQueryable<TEntityType> => result.AsQueryable(),
                IEnumerable<TEntityType> => result,
                _ => null
            };

            return filtered is not null;
        }
        catch
        {
            filtered = null;
            return false;
        }
    }

    private static bool TryMatchesDocument(
        BsonValue document,
        BsonDocument filter,
        out bool isMatch)
    {
        foreach (var element in filter)
        {
            if (!TryMatchesElement(document, element, out var elementMatches))
            {
                isMatch = false;
                return false;
            }

            if (!elementMatches)
            {
                isMatch = false;
                return true;
            }
        }

        isMatch = true;
        return true;
    }

    private static bool TryMatchesElement(
        BsonValue document,
        BsonElement element,
        out bool isMatch)
    {
        switch (element.Name)
        {
            case "$and":
                if (element.Value is not BsonArray andConditions)
                {
                    isMatch = false;
                    return false;
                }

                foreach (var condition in andConditions)
                {
                    if (condition is not BsonDocument conditionDocument
                        || !TryMatchesDocument(document, conditionDocument, out var conditionMatches))
                    {
                        isMatch = false;
                        return false;
                    }

                    if (!conditionMatches)
                    {
                        isMatch = false;
                        return true;
                    }
                }

                isMatch = true;
                return true;

            case "$or":
                if (element.Value is not BsonArray orConditions)
                {
                    isMatch = false;
                    return false;
                }

                foreach (var condition in orConditions)
                {
                    if (condition is not BsonDocument conditionDocument
                        || !TryMatchesDocument(document, conditionDocument, out var conditionMatches))
                    {
                        isMatch = false;
                        return false;
                    }

                    if (conditionMatches)
                    {
                        isMatch = true;
                        return true;
                    }
                }

                isMatch = false;
                return true;

            case "$nor":
                if (element.Value is not BsonArray norConditions)
                {
                    isMatch = false;
                    return false;
                }

                foreach (var condition in norConditions)
                {
                    if (condition is not BsonDocument conditionDocument
                        || !TryMatchesDocument(document, conditionDocument, out var conditionMatches))
                    {
                        isMatch = false;
                        return false;
                    }

                    if (conditionMatches)
                    {
                        isMatch = false;
                        return true;
                    }
                }

                isMatch = true;
                return true;

            default:
                return TryMatchesField(document, element.Name, element.Value, out isMatch);
        }
    }

    private static bool TryMatchesField(
        BsonValue document,
        string path,
        BsonValue condition,
        out bool isMatch)
    {
        var exists = TryResolvePath(document, path, out var fieldValue);

        if (condition is not BsonDocument conditionDocument
            || conditionDocument.ElementCount == 0
            || !conditionDocument.Names.All(name => name.StartsWith("$", StringComparison.Ordinal)))
        {
            isMatch = exists && BsonValuesEqual(fieldValue, condition);
            return true;
        }

        foreach (var operation in conditionDocument)
        {
            if (!TryApplyOperation(exists, fieldValue, operation, out var operationMatch))
            {
                isMatch = false;
                return false;
            }

            if (!operationMatch)
            {
                isMatch = false;
                return true;
            }
        }

        isMatch = true;
        return true;
    }

    private static bool TryApplyOperation(
        bool exists,
        BsonValue fieldValue,
        BsonElement operation,
        out bool isMatch)
    {
        switch (operation.Name)
        {
            case "$eq":
                isMatch = exists && BsonValuesEqual(fieldValue, operation.Value);
                return true;

            case "$ne":
                isMatch = !exists || !BsonValuesEqual(fieldValue, operation.Value);
                return true;

            case "$gt":
                isMatch = exists && fieldValue.CompareTo(operation.Value) > 0;
                return true;

            case "$gte":
                isMatch = exists && fieldValue.CompareTo(operation.Value) >= 0;
                return true;

            case "$lt":
                isMatch = exists && fieldValue.CompareTo(operation.Value) < 0;
                return true;

            case "$lte":
                isMatch = exists && fieldValue.CompareTo(operation.Value) <= 0;
                return true;

            case "$in":
                if (operation.Value is not BsonArray inArray)
                {
                    isMatch = false;
                    return false;
                }

                isMatch = exists && inArray.Any(value => BsonValuesEqual(fieldValue, value));
                return true;

            case "$nin":
                if (operation.Value is not BsonArray ninArray)
                {
                    isMatch = false;
                    return false;
                }

                isMatch = !exists || !ninArray.Any(value => BsonValuesEqual(fieldValue, value));
                return true;

            case "$exists":
                isMatch = exists == operation.Value.ToBoolean();
                return true;

            case "$regex":
                if (!exists)
                {
                    isMatch = false;
                    return true;
                }

                if (operation.Value is not BsonRegularExpression regex)
                {
                    isMatch = false;
                    return false;
                }

                var regexOptions = ParseRegexOptions(regex.Options);
                isMatch = Regex.IsMatch(fieldValue.AsString, regex.Pattern, regexOptions);
                return true;

            case "$not":
                if (!exists || operation.Value is not BsonDocument notCondition)
                {
                    isMatch = false;
                    return false;
                }

                if (!TryMatchesField(
                        new BsonDocument("value", fieldValue),
                        "value",
                        notCondition,
                        out var notMatch))
                {
                    isMatch = false;
                    return false;
                }

                isMatch = !notMatch;
                return true;

            case "$elemMatch":
                if (!exists || fieldValue is not BsonArray elements || operation.Value is not BsonDocument elementFilter)
                {
                    isMatch = false;
                    return true;
                }

                foreach (var element in elements)
                {
                    if (element is not BsonDocument elementDocument
                        || !TryMatchesDocument(elementDocument, elementFilter, out var elementMatch))
                    {
                        isMatch = false;
                        return false;
                    }

                    if (elementMatch)
                    {
                        isMatch = true;
                        return true;
                    }
                }

                isMatch = false;
                return true;

            default:
                isMatch = false;
                return false;
        }
    }

    private static bool TryResolvePath(BsonValue document, string path, out BsonValue value)
    {
        value = document;

        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (value is BsonDocument doc && doc.TryGetValue(segment, out var next))
            {
                value = next;
                continue;
            }

            value = BsonNull.Value;
            return false;
        }

        return true;
    }

    private static bool BsonValuesEqual(BsonValue left, BsonValue right)
    {
        if (left.IsBsonNull && right.IsBsonNull)
        {
            return true;
        }

        return left.Equals(right);
    }

    private static RegexOptions ParseRegexOptions(string options)
    {
        var regexOptions = RegexOptions.None;

        foreach (var option in options)
        {
            regexOptions |= option switch
            {
                'i' => RegexOptions.IgnoreCase,
                'm' => RegexOptions.Multiline,
                's' => RegexOptions.Singleline,
                _ => RegexOptions.None
            };
        }

        return regexOptions;
    }
}
