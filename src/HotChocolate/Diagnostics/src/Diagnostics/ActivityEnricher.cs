using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;
using GreenDonut;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Diagnostics;

/// <summary>
/// The activity enricher is used to add information to the activity spans.
/// You can inherit from this class and override the enricher methods to provide more or
/// less information.
/// </summary>
public class ActivityEnricher
{
    private readonly InstrumentationOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="ActivityEnricher"/>.
    /// </summary>
    /// <param name="stringBuilderPoolPool"></param>
    /// <param name="options"></param>
    protected ActivityEnricher(
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
                activity.DisplayName = "GraphQL HTTP POST";
                break;
            case HttpRequestKind.HttpMultiPart:
                activity.DisplayName = "GraphQL HTTP POST MultiPart";
                break;
            case HttpRequestKind.HttpGet:
                activity.DisplayName = "GraphQL HTTP GET";
                break;
            case HttpRequestKind.HttpGetSchema:
                activity.DisplayName = "GraphQL HTTP GET SDL";
                break;
        }

        if (_options.RenameRootActivity)
        {
            UpdateRootActivityName(activity, $"Begin {activity.DisplayName}");
        }

        activity.SetTag("graphql.http.kind", kind);

        var isDefault = false;
        if (!(context.Items.TryGetValue(SchemaName, out var value) &&
            value is string schemaName))
        {
            schemaName = Schema.DefaultName.Value;
            isDefault = true;
        }

        activity.SetTag("graphql.schema.name", schemaName);
        activity.SetTag("graphql.schema.isDefault", isDefault);
    }

    public virtual void EnrichSingleRequest(
        HttpContext context,
        GraphQLRequest request,
        Activity activity)
    {
        activity.SetTag("graphql.http.request.type", "single");

        if (request.QueryId is not null &&
            (_options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
        {
            activity.SetTag("graphql.http.request.query.id", request.QueryId);
        }

        if (request.QueryHash is not null &&
            (_options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
        {
            activity.SetTag("graphql.http.request.query.hash", request.QueryHash);
        }

        if (request.Query is not null &&
            (_options.RequestDetails & RequestDetails.Query) == RequestDetails.Query)
        {
            activity.SetTag("graphql.http.request.query.body", request.Query.Print());
        }

        if (request.OperationName is not null &&
            (_options.RequestDetails & RequestDetails.Operation) == RequestDetails.Operation)
        {
            activity.SetTag("graphql.http.request.operation", request.OperationName);
        }

        if (request.Variables is not null &&
            (_options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
        {
            var variables = new ObjectValueNode(
                request.Variables.Select(
                    t => new ObjectFieldNode(
                        null,
                        new NameNode(t.Key),
                        t.Value is null
                            ? NullValueNode.Default
                            : (IValueNode)t.Value))
                    .ToArray());

            EnrichRequestVariables(context, request, variables, activity);
        }

        if (request.Extensions is not null &&
            (_options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
        {
            EnrichRequestExtensions(context, request, request.Extensions, activity);
        }
    }

    public virtual void EnrichBatchRequest(
        HttpContext context,
        IReadOnlyList<GraphQLRequest> batch,
        Activity activity)
    {
        activity.SetTag("graphql.http.request.type", "batch");

        for (var i = 0; i < batch.Count; i++)
        {
            GraphQLRequest request = batch[i];

            if (request.QueryId is not null &&
            (_options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
            {
                activity.SetTag($"graphql.http.request[{i}].query.id", request.QueryId);
            }

            if (request.QueryHash is not null &&
                (_options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
            {
                activity.SetTag($"graphql.http.request[{i}].query.hash", request.QueryHash);
            }

            if (request.Query is not null &&
                (_options.RequestDetails & RequestDetails.Query) == RequestDetails.Query)
            {
                activity.SetTag($"graphql.http.request[{i}].query.body", request.Query.Print());
            }

            if (request.OperationName is not null &&
                (_options.RequestDetails & RequestDetails.Operation) == RequestDetails.Operation)
            {
                activity.SetTag($"graphql.http.request[{i}].operation", request.OperationName);
            }

            if (request.Variables is not null &&
                (_options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
            {
                var variables = new ObjectValueNode(
                    request.Variables.Select(
                        t => new ObjectFieldNode(
                            null,
                            new NameNode(t.Key),
                            t.Value is null
                                ? NullValueNode.Default
                                : (IValueNode)t.Value))
                        .ToArray());

                EnrichBatchVariables(context, request, variables, i, activity);
            }

            if (request.Extensions is not null &&
                (_options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
            {
                EnrichBatchExtensions(context, request, request.Extensions, i, activity);
            }
        }
    }

    public virtual void EnrichOperationBatchRequest(
        HttpContext context,
        GraphQLRequest request,
        IReadOnlyList<string> operations,
        Activity activity)
    {
        activity.SetTag("graphql.http.request.type", "operationBatch");

        if (request.QueryId is not null &&
            (_options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
        {
            activity.SetTag("graphql.http.request.query.id", request.QueryId);
        }

        if (request.QueryHash is not null &&
            (_options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
        {
            activity.SetTag("graphql.http.request.query.hash", request.QueryHash);
        }

        if (request.Query is not null &&
            (_options.RequestDetails & RequestDetails.Query) == RequestDetails.Query)
        {
            activity.SetTag("graphql.http.request.query.body", request.Query.Print());
        }

        if (request.OperationName is not null &&
            (_options.RequestDetails & RequestDetails.Operation) == RequestDetails.Operation)
        {
            activity.SetTag("graphql.http.request.operations", string.Join(" -> ", operations));
        }

        if (request.Variables is not null &&
            (_options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
        {
            var variables = new ObjectValueNode(
                request.Variables.Select(
                    t => new ObjectFieldNode(
                        null,
                        new NameNode(t.Key),
                        t.Value is null
                            ? NullValueNode.Default
                            : (IValueNode)t.Value))
                    .ToArray());

            EnrichRequestVariables(context, request, variables, activity);
        }

        if (request.Extensions is not null &&
            (_options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
        {
            EnrichRequestExtensions(context, request, request.Extensions, activity);
        }
    }

    protected virtual void EnrichRequestVariables(
        HttpContext context,
        GraphQLRequest request,
        ObjectValueNode variables,
        Activity activity)
    {
        activity.SetTag("graphql.http.request.variables", variables.Print());
    }

    protected virtual void EnrichBatchVariables(
        HttpContext context,
        GraphQLRequest request,
        ObjectValueNode variables,
        int index,
        Activity activity)
    {
        activity.SetTag($"graphql.http.request[{index}].variables", variables.Print());
    }

    protected virtual void EnrichRequestExtensions(
        HttpContext context,
        GraphQLRequest request,
        IReadOnlyDictionary<string, object?> extensions,
        Activity activity)
    {
        try
        {
            activity.SetTag(
                "graphql.http.request.extensions",
                JsonSerializer.Serialize(extensions));
        }
        catch
        {
            // Ignore any errors
        }
    }

    protected virtual void EnrichBatchExtensions(
        HttpContext context,
        GraphQLRequest request,
        IReadOnlyDictionary<string, object?> extensions,
        int index,
        Activity activity)
    {
        try
        {
            activity.SetTag(
                $"graphql.http.request[{index}].extensions",
                JsonSerializer.Serialize(extensions));
        }
        catch
        {
            // Ignore any errors
        }
    }

    public virtual void EnrichHttpRequestError(
        HttpContext context,
        IError error,
        Activity activity)
        => EnrichError(error, activity);

    public virtual void EnrichHttpRequestError(
        HttpContext context,
        Exception exception,
        Activity activity)
    {

    }

    public virtual void EnrichParseHttpRequest(HttpContext context, Activity activity)
    {
        activity.DisplayName = "Parse HTTP Request";

        if (_options.RenameRootActivity)
        {
            UpdateRootActivityName(activity, $"Begin {activity.DisplayName}");
        }
    }

    public virtual void EnrichParserErrors(HttpContext context, IError error, Activity activity)
        => EnrichError(error, activity);

    public virtual void EnrichFromatHttpResponse(HttpContext context, Activity activity)
    {
        activity.DisplayName = "Format HTTP Response";
    }

    public virtual void EnrichExecuteRequest(IRequestContext context, Activity activity)
    {
        var operationDisplayName = CreateOperationDisplayName(context);

        if (_options.RenameRootActivity && operationDisplayName is not null)
        {
            UpdateRootActivityName(activity, operationDisplayName);
        }

        activity.DisplayName = operationDisplayName ?? "Execute Request";
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
        }
    }

    protected virtual string? CreateOperationDisplayName(IRequestContext context)
    {
        if (context.Operation is { } operation)
        {
            StringBuilder displayName = StringBuilderPool.Get();

            try
            {
                ISelectionSet rootSelectionSet = operation.GetRootSelectionSet();

                displayName.Append('{');
                displayName.Append(' ');

                foreach (ISelection selection in rootSelectionSet.Selections.Take(3))
                {
                    if (displayName.Length > 2)
                    {
                        displayName.Append(' ');
                    }

                    displayName.Append(selection.ResponseName);
                }

                if (rootSelectionSet.Selections.Count > 3)
                {
                    displayName.Append(' ');
                    displayName.Append('.');
                    displayName.Append('.');
                    displayName.Append('.');
                }

                displayName.Append(' ');
                displayName.Append('}');

                if (operation.Name is { } name)
                {
                    displayName.Insert(0, ' ');
                    displayName.Insert(0, name.Value);
                }

                displayName.Insert(0, ' ');
                displayName.Insert(0, operation.Definition.Operation.ToString().ToLowerInvariant());

                return displayName.ToString();
            }
            finally
            {
                StringBuilderPool.Return(displayName);
            }
        }

        return null;
    }

    private void UpdateRootActivityName(Activity activity, string displayName)
    {
        Activity current = activity;

        while (current.Parent is not null)
        {
            current = current.Parent;
        }

        if (current != activity)
        {
            current.DisplayName = CreateRootActivityName(activity, current, displayName);
        }
    }

    protected virtual string CreateRootActivityName(
        Activity activity,
        Activity root,
        string displayName)
    {
        const string key = "originalDisplayName";

        if (root.GetCustomProperty(key) is not string rootDisplayName)
        {
            rootDisplayName = root.DisplayName;
            root.SetCustomProperty(key, rootDisplayName);
        }

        return $"{rootDisplayName}: {displayName}";
    }

    public virtual void EnrichParseDocument(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Parse Document";

        if (_options.RenameRootActivity)
        {
            UpdateRootActivityName(activity, $"Begin {activity.DisplayName}");
        }
    }

    public virtual void EnrichSyntaxError(
        IRequestContext context,
        Activity activity,
        IError error)
        => EnrichError(error, activity);

    public virtual void EnrichValidateDocument(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Validate Document";

        if (_options.RenameRootActivity)
        {
            UpdateRootActivityName(activity, $"Begin {activity.DisplayName}");
        }

        activity.SetTag("graphql.document.id", context.DocumentId);
        activity.SetTag("graphql.document.hash", context.DocumentHash);
    }

    public virtual void EnrichValidationError(
        IRequestContext context,
        Activity activity,
        IError error)
        => EnrichError(error, activity);

    public virtual void EnrichAnalyzeOperationComplexity(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Analyze Operation Complexity";
    }

    public virtual void EnrichCoerceVariables(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Coerce Variable";
    }

    public virtual void EnrichCompileOperation(IRequestContext context, Activity activity)
    {
        activity.DisplayName = "Compile Operation";
    }

    public virtual void EnrichExecuteOperation(IRequestContext context, Activity activity)
    {
        activity.DisplayName =
            context.Operation?.Name?.Value is { } op
                ? $"Execute Operation {op}"
                : "Execute Operation";
    }

    public virtual void EnrichResolveFieldValue(IMiddlewareContext context, Activity activity)
    {
        string path;
        string hierarchy;
        BuildPath();

        IFieldSelection selection = context.Selection;
        FieldCoordinate coordinate = selection.Field.Coordinate;

        activity.DisplayName = path;
        activity.SetTag("graphql.selection.name", selection.ResponseName.Value);
        activity.SetTag("graphql.selection.type", selection.Field.Type.Print());
        activity.SetTag("graphql.selection.path", path);
        activity.SetTag("graphql.selection.hierarchy", hierarchy);
        activity.SetTag("graphql.selection.field.name", coordinate.FieldName.Value);
        activity.SetTag("graphql.selection.field.coordinate", coordinate.ToString());
        activity.SetTag("graphql.selection.field.declaringType", coordinate.TypeName.Value);
        activity.SetTag("graphql.selection.field.isDeprecated", selection.Field.IsDeprecated);

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
            } while (current is not null && !current.IsRoot);

            path = p.ToString();
            hierarchy = h.ToString();

            StringBuilderPool.Return(p);
            StringBuilderPool.Return(h);
            StringBuilderPool.Return(index);
        }
    }

    public virtual void EnrichResolverError(
        IMiddlewareContext context,
        IError error,
        Activity activity)
        => EnrichError(error, activity);

    public virtual void EnrichDataLoaderBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys,
        Activity activity)
        where TKey : notnull
    {
        activity.DisplayName = $"Execute {dataLoader.GetType().Name} Batch";
        activity.SetTag("graphql.dataLoader.keys.count", keys.Count);

        if (_options.IncludeDataLoaderKeys)
        {
            var temp = keys.Select(t => t.ToString()).ToArray();
            activity.SetTag("graphql.dataLoader.keys", temp);
        }
    }

    protected virtual void EnrichError(IError error, Activity activity)
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
    }
}
