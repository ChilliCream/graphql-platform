using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Language.Utilities;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Diagnostics;

public class ActivityEnricher
{
    private readonly InstrumentationOptions _options;

    public ActivityEnricher(
        ObjectPool<StringBuilder> stringBuilderPoolPool,
        InstrumentationOptions options)
    {
        StringBuilderPool = stringBuilderPoolPool;
        _options = options;
    }


    /// <summary>
    /// Gets the <see cref="StringBuilder"/> pool used by this enricher.
    /// </summary>
    protected ObjectPool<StringBuilder> StringBuilderPool { get; }

    public virtual void EnrichExecuteHttpRequest(
        HttpContext context,
        HttpRequestKind kind,
        Activity activity)
    {
        switch (kind)
        {
            case HttpRequestKind.HttpPost:
                activity.DisplayName = "GRAPHQL HTTP POST";
                break;
            case HttpRequestKind.HttpMultiPart:
                activity.DisplayName = "GRAPHQL HTTP POST MultiPart";
                break;
            case HttpRequestKind.HttpGet:
                activity.DisplayName = "GRAPHQL HTTP GET";
                break;
            case HttpRequestKind.HttpGetSchema:
                activity.DisplayName = "GRAPHQL HTTP GET SDL";
                break;
        }

        activity.SetTag("graphql.http.kind", kind);
        
    }

    public virtual void EnrichExecuteRequest(IRequestContext context, Activity activity)
    {
        activity.DisplayName = context.Operation?.Name?.Value ?? "Execute Request";
        activity.SetTag("graphql.document.id", context.DocumentId);
        activity.SetTag("graphql.document.hash", context.DocumentHash);
        activity.SetTag("graphql.document.valid", context.IsValidDocument);
        activity.SetTag("graphql.operation.id", context.OperationId);
        activity.SetTag("graphql.operation.kind", context.Operation?.Type);
        activity.SetTag("graphql.operation.name", context.Operation?.Name?.Value);

        if (_options.IncludeDocument && context.Document is not null)
        {
            activity.SetTag("graphql.document.body", context.Document.Print());
        }

        if (context.Result is IQueryResult result)
        {
            var errorCount = result.Errors?.Count ?? 0;
            activity.SetTag("graphql.errors.count", errorCount);
            activity.SetStatus(result.Data is not null
                ? ActivityStatusCode.Ok
                : ActivityStatusCode.Error);
        }
    }

    public virtual void EnrichParseDocument(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Parse Document";
        activity.SetStatus(context.Document is null
            ? ActivityStatusCode.Error
            : ActivityStatusCode.Ok);
    }

    public virtual void EnrichSyntaxError(
        IRequestContext context,
        Activity activity,
        IError error)
        => EnrichError(activity, error);

    public virtual void EnrichValidateDocument(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Validate Document";
        activity.SetTag("graphql.document.id", context.DocumentId);
        activity.SetTag("graphql.document.hash", context.DocumentHash);
        activity.SetStatus(
            context.IsValidDocument
                ? ActivityStatusCode.Ok
                : ActivityStatusCode.Error);
    }

    public virtual void EnrichValidationError(
        IRequestContext context,
        Activity activity,
        IError error)
        => EnrichError(activity, error);

    public virtual void EnrichAnalyzeOperationComplexity(IRequestContext context, Activity activity)
    {
        var allowed = context.ContextData.ContainsKey(OperationComplexityAllowed);
        activity.DisplayName = "Analyze Operation Complexity";
        activity.SetStatus(allowed ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
    }

    public virtual void EnrichCoerceVariables(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Coerce Variable";
    }

    public virtual void EnrichCompileOperation(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Compile Operation";
    }

    public virtual void EnrichBuildQueryPlan(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Build Query Plan";
    }

    public virtual void EnrichResolver(IMiddlewareContext context, Activity activity)
    {
        var path = string.Empty;
        var hierarchy = string.Empty;
        BuildPath();

        FieldCoordinate coordinate = context.Selection.Field.Coordinate;
        activity.DisplayName = path;
        activity.SetTag("graphql.selection.name", context.Selection.ResponseName.Value);
        activity.SetTag("graphql.selection.parentType", coordinate.TypeName.Value);
        activity.SetTag("graphql.selection.coordinate", coordinate.ToString());
        activity.SetTag("graphql.selection.type", context.Selection.Field.Type.Print());
        activity.SetTag("graphql.selection.path", path);
        activity.SetTag("graphql.selection.hierarchy", hierarchy);
        activity.SetStatus(ActivityStatusCode.Ok);

        void BuildPath()
        {
            StringBuilder p = StringBuilderPool.Get();
            StringBuilder h = StringBuilderPool.Get();
            StringBuilder index = StringBuilderPool.Get();

            Path? current = context.Path;

            do
            {
                if (current is NamePathSegment n)
                {
                    p.Insert(0, '/');
                    h.Insert(0, '/');
                    p.Insert(1, n.Name.Value);
                    h.Insert(1, n.Name.Value);

                    if (index.Length > 0)
                    {
                        p.Insert(1 + n.Name.Value.Length, index);
                    }

                    index.Clear();
                }

                if (current is IndexerPathSegment i)
                {
                    var number = i.Index.ToString();
                    index.Insert(0, '[');
                    index.Insert(1, number);
                    index.Insert(1 + number.Length, ']');
                }

                current = current.Parent;
            } while (current is not null && current is not RootPathSegment);

            StringBuilderPool.Return(p);
            StringBuilderPool.Return(h);
            StringBuilderPool.Return(index);
        }
    }

    public virtual void EnrichResolverError(
        IMiddlewareContext context,
        Activity activity,
        IError error)
        => EnrichError(activity, error);

    protected virtual void EnrichError(Activity activity, IError error)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("graphql.error.message", error.Message),
            new("graphql.error.code", error.Code)
        };

        if (error.Locations is { Count: > 0 })
        {
            if (error.Locations.Count == 1)
            {
                tags.Add(new($"graphql.error.location.column", error.Locations[0].Column));
                tags.Add(new($"graphql.error.location.line", error.Locations[0].Line));
            }
            else
            {
                for (var i = 0; i < error.Locations.Count; i++)
                {
                    tags.Add(new($"graphql.error.location[{i}].column", error.Locations[i].Column));
                    tags.Add(new($"graphql.error.location[{i}].line", error.Locations[i].Line));
                }
            }
        }

        activity.AddEvent(new("Error", tags: new(tags)));

        if (activity.OperationName.EqualsOrdinal(
            nameof(ActivityExecutionDiagnosticListener.ResolveFieldValue)))
        {
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }
}
