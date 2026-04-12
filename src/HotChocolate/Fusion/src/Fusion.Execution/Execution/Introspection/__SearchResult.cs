using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __SearchResult : ITypeResolverInterceptor
{
    public void OnApplyResolver(string fieldName, IFeatureCollection features)
    {
        switch (fieldName)
        {
            case "cursor":
                features.Set(new ResolveFieldValue(Cursor));
                break;

            case "coordinate":
                features.Set(new ResolveFieldValue(Coordinate));
                break;

            case "definition":
                features.Set(new ResolveFieldValue(Definition));
                break;

            case "pathsToRoot":
                features.Set(new ResolveFieldValue(PathsToRoot));
                break;

            case "score":
                features.Set(new ResolveFieldValue(Score));
                break;
        }
    }

    public static void Cursor(FieldContext context)
        => context.WriteValue(context.Parent<SearchResultData>().Cursor);

    public static void Coordinate(FieldContext context)
        => context.WriteValue(context.Parent<SearchResultData>().Coordinate.ToString());

    public static void Definition(FieldContext context)
    {
        var data = context.Parent<SearchResultData>();
        context.FieldResult.CreateObjectValue(context.Selection, context.IncludeFlags);
        context.AddRuntimeResult(data.Definition);
    }

    public static void PathsToRoot(FieldContext context)
    {
        var data = context.Parent<SearchResultData>();
        var paths = data.PathsToRoot;
        var list = context.FieldResult.CreateListValue(paths.Count);

        var index = 0;
        foreach (var element in list.EnumerateArray())
        {
            element.SetStringValue(paths[index++]);
        }
    }

    public static void Score(FieldContext context)
    {
        var data = context.Parent<SearchResultData>();
        if (data.Score.HasValue)
        {
            context.WriteFloatValue(data.Score.Value);
        }
    }
}
