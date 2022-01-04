using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Language.Utilities;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Diagnostics;

public class ActivityEnricher
{
    private readonly InstrumentationOptions _options;

    public ActivityEnricher(InstrumentationOptions options)
    {
        _options = options;
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
            activity.SetStatus(errorCount is 0 ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
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
        FieldCoordinate coordinate = context.Selection.Field.Coordinate;
        activity.DisplayName = $"{context.Path}";
        activity.SetTag("graphql.selection.name", context.Selection.ResponseName.Value);
        activity.SetTag("graphql.selection.parentType", coordinate.TypeName.Value);
        activity.SetTag("graphql.selection.coordinate", coordinate.ToString());
        activity.SetTag("graphql.selection.type", context.Selection.Field.Type.Print());
        activity.SetTag("graphql.selection.path", context.Path.ToString());
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    public virtual void EnrichResolverError(
        IMiddlewareContext context,
        Activity activity,
        IError error)
        => EnrichError(activity, error);

    protected virtual void EnrichError(Activity activity, IError error)
    {
        var tags = new List<KeyValuePair<string, object?>>();
        tags.Add(new("graphql.error.message", error.Message));
        tags.Add(new("graphql.error.code", error.Code));

        if (error.Locations is { Count: > 0 })
        {
            for (var i = 0; i < error.Locations.Count; i++)
            {
                tags.Add(new($"graphql.error.location.{i}.column", error.Locations[i].Column));
                tags.Add(new($"graphql.error.location.{i}.line", error.Locations[i].Line));
            }
        }

        activity.AddEvent(new("Error", tags: new(tags)));
        activity.SetStatus(ActivityStatusCode.Error);
    }
}
