using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.Extensions.DependencyInjection;

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
                features.Set(new AsyncResolveFieldValue(PathsToRootAsync));
                break;

            case "score":
                features.Set(new ResolveFieldValue(Score));
                break;
        }
    }

    public static void Cursor(FieldContext context)
        => context.WriteValue(context.Parent<SchemaSearchResult>().Cursor);

    public static void Coordinate(FieldContext context)
        => context.WriteValue(context.Parent<SchemaSearchResult>().Coordinate.ToString());

    public static void Definition(FieldContext context)
    {
        var result = context.Parent<SchemaSearchResult>();

        var member = context.Schema.GetMember(result.Coordinate);
        var objectType = SchemaDefinitionTypeResolver.ResolveObjectType(context.Schema, member);
        context.FieldResult.CreateObjectValue(context.Selection, objectType, context.IncludeFlags);
        context.AddRuntimeResult(member);
    }

    public static async ValueTask PathsToRootAsync(FieldContext context)
    {
        var result = context.Parent<SchemaSearchResult>();
        var provider = context.Schema.Services.GetRequiredService<ISchemaSearchProvider>();
        var paths = await provider.GetPathsToRootAsync(
            result.Coordinate,
            context.RequestAborted)
            .ConfigureAwait(false);

        var outerList = context.FieldResult.CreateListValue(paths.Count);

        var outerIndex = 0;
        foreach (var outerElement in outerList.EnumerateArray())
        {
            var path = paths[outerIndex++].ToStringArray();
            var innerList = outerElement.CreateListValue(path.Length);

            var innerIndex = 0;
            foreach (var innerElement in innerList.EnumerateArray())
            {
                innerElement.SetStringValue(path[innerIndex++]);
            }
        }
    }

    public static void Score(FieldContext context)
    {
        var result = context.Parent<SchemaSearchResult>();

        if (result.Score.HasValue)
        {
            context.WriteFloatValue(result.Score.Value);
        }
    }
}
