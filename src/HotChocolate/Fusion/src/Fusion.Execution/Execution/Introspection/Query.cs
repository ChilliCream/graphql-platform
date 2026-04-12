using System.Diagnostics;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;
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
                features.Set(new ResolveFieldValue(Search));
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

    public static void Search(FieldContext context)
    {
        var provider = context.Schema.Services.GetService<ISchemaSearchProvider>();

        if (provider is null)
        {
            context.FieldResult.CreateListValue(0);
            return;
        }

        var query = context.ArgumentValue<StringValueNode>("query").Value;
        var first = int.Parse(context.ArgumentValue<IntValueNode>("first").Value);

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

        var results = provider.SearchAsync(query, first, after, minScore).AsTask().GetAwaiter().GetResult();

        var searchResults = new List<SearchResultData>(results.Count);

        foreach (var result in results)
        {
            var definition = SchemaCoordinateResolver.Resolve(context.Schema, result.Coordinate);

            if (definition is null)
            {
                continue;
            }

            var paths = provider.GetPathsToRootAsync(result.Coordinate, maxPaths: 5).AsTask().GetAwaiter().GetResult();

            var pathStrings = new List<string>(paths.Count);

            foreach (var path in paths)
            {
                pathStrings.Add(string.Join(" > ", path.Select(c => c.ToString())));
            }

            searchResults.Add(new SearchResultData
            {
                Coordinate = result.Coordinate,
                Definition = definition,
                PathsToRoot = pathStrings,
                Score = result.Score,
                Cursor = result.Cursor
            });
        }

        var list = context.FieldResult.CreateListValue(searchResults.Count);

        var i = 0;
        foreach (var element in list.EnumerateArray())
        {
            context.AddRuntimeResult(searchResults[i++]);
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

            var definition = SchemaCoordinateResolver.Resolve(context.Schema, coordinate.Value);

            if (definition is not null)
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
