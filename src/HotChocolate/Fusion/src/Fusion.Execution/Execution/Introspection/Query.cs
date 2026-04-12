using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Introspection;

internal class Query : ITypeResolverInterceptor
{
    private readonly bool _enableSemanticIntrospection;

    public Query(bool enableSemanticIntrospection = false)
    {
        _enableSemanticIntrospection = enableSemanticIntrospection;
    }

    public void OnApplyResolver(string fieldName, IFeatureCollection features)
    {
        switch (fieldName)
        {
            case "__schema":
                features.Set(new ResolveFieldValue(Schema));
                break;

            case "__type":
                features.Set(new ResolveFieldValue(Type));
                break;

            case "__search" when _enableSemanticIntrospection:
                features.Set(new AsyncResolveFieldValue(SearchAsync));
                break;

            case "__definitions" when _enableSemanticIntrospection:
                features.Set(new ResolveFieldValue(Definitions));
                break;
        }
    }

    public static void Schema(FieldContext context)
    {
        context.FieldResult.CreateObjectValue(context.Selection, context.IncludeFlags);
        context.AddRuntimeResult(context.Schema);
    }

    public static void Type(FieldContext context)
    {
        var name = context.ArgumentValue<StringValueNode>("name");
        if (context.Schema.Types.TryGetType(name.Value, out var type))
        {
            context.FieldResult.CreateObjectValue(context.Selection, context.IncludeFlags);
            context.AddRuntimeResult(type);
        }
    }

    private const int MaxFirstLimit = 150;

    public static async ValueTask SearchAsync(FieldContext context)
    {
        var provider = context.Schema.Services.GetService<ISchemaSearchProvider>();

        if (provider is null)
        {
            context.FieldResult.CreateListValue(0);
            return;
        }

        var query = context.ArgumentValue<StringValueNode>("query").Value;
        var first = int.Parse(context.ArgumentValue<IntValueNode>("first").Value);

        if (first <= 0)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("The `first` argument must be greater than zero.")
                    .Build());
        }

        if (first > MaxFirstLimit)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"The `first` argument must not exceed {MaxFirstLimit}.")
                    .Build());
        }

        string? after = null;
        var afterNode = context.ArgumentValue<IValueNode>("after");

        if (afterNode is StringValueNode afterString)
        {
            after = afterString.Value;
        }

        float? minScore = null;
        var minScoreNode = context.ArgumentValue<IValueNode>("min_score");

        if (minScoreNode is FloatValueNode floatNode)
        {
            minScore = float.Parse(floatNode.Value, System.Globalization.CultureInfo.InvariantCulture);
        }
        else if (minScoreNode is IntValueNode intNode)
        {
            minScore = float.Parse(intNode.Value, System.Globalization.CultureInfo.InvariantCulture);
        }

        IReadOnlyList<SchemaSearchResult> results;

        try
        {
            results = await provider.SearchAsync(
                query,
                first,
                after,
                minScore,
                context.RequestAborted).ConfigureAwait(false);
        }
        catch (InvalidSearchCursorException)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("The value of `after` is not a valid cursor.")
                    .Build());
        }
        catch (SearchQueryTooLargeException)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("The search query exceeds the maximum allowed length.")
                    .Build());
        }

        var list = context.FieldResult.CreateListValue(results.Count);

        var i = 0;
        foreach (var element in list.EnumerateArray())
        {
            context.AddRuntimeResult(results[i++]);
            element.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }

    public static void Definitions(FieldContext context)
    {
        var coordinatesNode = context.ArgumentValue<ListValueNode>("coordinates");
        var definitions = new List<object>(coordinatesNode.Items.Count);

        foreach (var item in coordinatesNode.Items)
        {
            if (item is not StringValueNode coordinateString)
            {
                continue;
            }

            if (!SchemaCoordinate.TryParse(coordinateString.Value, out var coordinate))
            {
                continue;
            }

            if (context.Schema.TryGetMember(coordinate.Value, out var definition))
            {
                definitions.Add(definition);
            }
        }

        var list = context.FieldResult.CreateListValue(definitions.Count);

        var i = 0;
        foreach (var element in list.EnumerateArray())
        {
            context.AddRuntimeResult(definitions[i++]);
            element.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }
}
