using System.Collections.Generic;
using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Diagnostics;

public class ActivityEnricher
{
    public virtual void EnrichExecuteRequest(IRequestContext context, Activity activity)
    {
        if (context.Operation is not null)
        {
            activity.SetTag("graphql.request.document.id", context.DocumentId);
            activity.SetTag("graphql.request.document.hash", context.DocumentHash);
            activity.SetTag("graphql.request.document.valid", context.IsValidDocument);

            activity.SetTag("graphql.request.operation.id", context.OperationId);
            activity.SetTag("graphql.request.operation.kind", context.Operation.Type);

            if (context.Operation.Name is not null)
            {
                activity.DisplayName = context.Operation.Name;
                activity.SetTag("graphql.request.operation.name", context.Operation.Name);
            }
        }

        if (context.Result is IQueryResult result)
        {
            activity.SetTag("graphql.request.errors.count", result.Errors?.Count ?? 0);
            activity.SetTag("graphql.request.status", (result.Errors?.Count ?? 0) == 0 ? "success" : "error");
        }
    }

    public virtual void EnrichParseDocument(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Parse";
        if (context.Document is not null)
        {

        }
    }

    public virtual void EnrichSyntaxError(IRequestContext context, IError error, ICollection<KeyValuePair<string, object?>> tags)
    {
        tags.Add(new("graphql.error.message", error.Message));
        tags.Add(new("graphql.error.code", error.Code));

        if (error.Locations is { Count: > 0 })
        {
            for (int i = 0; i < error.Locations.Count; i++)
            {
                tags.Add(new($"graphql.error.location.{i}.column", error.Locations[i].Column));
                tags.Add(new($"graphql.error.location.{i}.line", error.Locations[i].Line));
            }
        }
    }

    public virtual void EnrichValidateDocument(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Validate";
        activity.SetTag("graphql.request.document.id", context.DocumentId);
        activity.SetTag("graphql.request.document.hash", context.DocumentHash);
        activity.SetTag("graphql.request.document.valid", context.IsValidDocument);
    }

    public virtual void EnrichValidationErrors(IRequestContext context, IError errors, ICollection<KeyValuePair<string, object?>> tags)
    {
    }

    public virtual void EnrichResolver(IMiddlewareContext context, Activity activity)
    {
        activity.DisplayName = $"{context.Path}";
        activity.SetTag("graphql.selection.name", context.Selection.ResponseName);
        activity.SetTag("graphql.selection.coordinate", context.Selection.Field.Coordinate.ToString());
        activity.SetTag("graphql.selection.type", context.Selection.Field.Type.Print());
        activity.SetTag("graphql.selection.path", context.Path.ToString());
    }
}
