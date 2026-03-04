using System.Diagnostics;
using System.Runtime.CompilerServices;
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
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;
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
    private readonly ConditionalWeakTable<ISyntaxNode, string> _queryCache = [];

    /// <summary>
    /// Initializes a new instance of <see cref="ActivityEnricher"/>.
    /// </summary>
    /// <param name="stringBuilderPool"></param>
    /// <param name="options"></param>
    protected ActivityEnricher(
        ObjectPool<StringBuilder> stringBuilderPool,
        InstrumentationOptions options)
    {
        StringBuilderPool = stringBuilderPool;
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

        activity.SetTag(GraphQL.Http.Kind, kind);

        var isDefault = false;
        if (!(context.Items.TryGetValue(SchemaName, out var value)
            && value is string schemaName))
        {
            schemaName = ISchemaDefinition.DefaultName;
            isDefault = true;
        }

        // TODO: Is this needed?
        activity.SetTag(GraphQL.Schema.Name, schemaName);
        activity.SetTag(GraphQL.Schema.IsDefault, isDefault);
    }

    public virtual void EnrichSingleRequest(
        HttpContext context,
        GraphQLRequest request,
        Activity activity)
    {
        activity.SetTag(GraphQL.Http.Request.Type, "single");

        if (request.DocumentId is not null
            && (_options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
        {
            activity.SetTag(GraphQL.Http.Request.QueryId, request.DocumentId.Value);
        }

        if (request.DocumentHash is not null
            && (_options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
        {
            activity.SetTag(GraphQL.Http.Request.QueryHash, request.DocumentHash.Value);
        }

        if (request.Document is not null
            && (_options.RequestDetails & RequestDetails.Query) == RequestDetails.Query)
        {
            if (!_queryCache.TryGetValue(request.Document, out var query))
            {
                query = request.Document.Print();
                _queryCache.Add(request.Document, query);
            }

            activity.SetTag(GraphQL.Http.Request.QueryBody, query);
        }

        if (request.OperationName is not null
            && (_options.RequestDetails & RequestDetails.Operation) == RequestDetails.Operation)
        {
            activity.SetTag(GraphQL.Http.Request.OperationName, request.OperationName);
        }

        if (request.Variables is not null
            && (_options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
        {
            EnrichRequestVariables(context, request, request.Variables, activity);
        }

        if (request.Extensions is not null
            && (_options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
        {
            EnrichRequestExtensions(context, request, request.Extensions, activity);
        }
    }

    public virtual void EnrichBatchRequest(
        HttpContext context,
        IReadOnlyList<GraphQLRequest> batch,
        Activity activity)
    {
        activity.SetTag(GraphQL.Http.Request.Type, "batch");

        for (var i = 0; i < batch.Count; i++)
        {
            var request = batch[i];

            if (request.DocumentId is not null
                && (_options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
            {
                activity.SetTag($"graphql.http.request[{i}].query.id", request.DocumentId.Value);
            }

            if (request.DocumentHash is not null
                && (_options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
            {
                activity.SetTag($"graphql.http.request[{i}].query.hash", request.DocumentHash.Value);
            }

            if (request.Document is not null
                && (_options.RequestDetails & RequestDetails.Query) == RequestDetails.Query)
            {
                activity.SetTag($"graphql.http.request[{i}].query.body", request.Document.Print());
            }

            if (request.OperationName is not null
                && (_options.RequestDetails & RequestDetails.Operation) == RequestDetails.Operation)
            {
                activity.SetTag($"graphql.http.request[{i}].operation", request.OperationName);
            }

            if (request.Variables is not null
                && (_options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
            {
                EnrichBatchVariables(context, request, request.Variables, i, activity);
            }

            if (request.Extensions is not null
                && (_options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
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
        activity.SetTag(GraphQL.Http.Request.Type, "operationBatch");

        if (request.DocumentId is not null
            && (_options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
        {
            activity.SetTag(GraphQL.Http.Request.QueryId, request.DocumentId.Value);
        }

        if (request.DocumentHash is not null
            && (_options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
        {
            activity.SetTag(GraphQL.Http.Request.QueryHash, request.DocumentHash.Value);
        }

        if (request.Document is not null
            && (_options.RequestDetails & RequestDetails.Query) == RequestDetails.Query)
        {
            activity.SetTag(GraphQL.Http.Request.QueryBody, request.Document.Print());
        }

        if (request.OperationName is not null
            && (_options.RequestDetails & RequestDetails.Operation) == RequestDetails.Operation)
        {
            activity.SetTag(GraphQL.Http.Request.Operations, string.Join(" -> ", operations));
        }

        if (request.Variables is not null
            && (_options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
        {
            EnrichRequestVariables(context, request, request.Variables, activity);
        }

        if (request.Extensions is not null
            && (_options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
        {
            EnrichRequestExtensions(context, request, request.Extensions, activity);
        }
    }

    protected virtual void EnrichRequestVariables(
        HttpContext context,
        GraphQLRequest request,
        JsonDocument variables,
        Activity activity)
        => activity.SetTag(GraphQL.Http.Request.Variables, variables.RootElement.ToString());

    protected virtual void EnrichBatchVariables(
        HttpContext context,
        GraphQLRequest request,
        JsonDocument variables,
        int index,
        Activity activity)
        => activity.SetTag($"graphql.http.request[{index}].variables", variables.RootElement.ToString());

    protected virtual void EnrichRequestExtensions(
        HttpContext context,
        GraphQLRequest request,
        JsonDocument extensions,
        Activity activity)
    {
        try
        {
            activity.SetTag(
                GraphQL.Http.Request.Extensions,
                extensions.RootElement.ToString());
        }
        catch
        {
            // Ignore any errors
        }
    }

    protected virtual void EnrichBatchExtensions(
        HttpContext context,
        GraphQLRequest request,
        JsonDocument extensions,
        int index,
        Activity activity)
    {
        try
        {
            activity.SetTag(
                $"graphql.http.request[{index}].extensions",
                extensions.RootElement.ToString());
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
        System.Exception exception,
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

    public virtual void EnrichFormatHttpResponse(HttpContext context, Activity activity)
    {
        activity.DisplayName = "Format HTTP Response";
    }

    public virtual void EnrichExecuteRequest(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);
        var documentInfo = context.OperationDocumentInfo;
        var operationDisplayName = CreateOperationDisplayName(context, operation);

        if (_options.RenameRootActivity && operationDisplayName is not null)
        {
            UpdateRootActivityName(activity, operationDisplayName);
        }

        activity.DisplayName = operationDisplayName ?? "Execute Request";
        activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);
        activity.SetTag(GraphQL.Document.Valid, documentInfo.IsValidated);
        activity.SetTag(GraphQL.Operation.Id, operation?.Id);

        if (operation is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[operation.Kind]);

            if (!string.IsNullOrEmpty(operation.Name))
            {
                activity.SetTag(GraphQL.Operation.Name, operation.Name);
            }
        }

        if (_options.IncludeDocument && documentInfo.Document is not null)
        {
            activity.SetTag(GraphQL.Document.Body, documentInfo.Document.Print());
        }

        if (context.Result is OperationResult result)
        {
            var errorCount = result.Errors.Count;
            activity.SetTag(GraphQL.Errors.Count, errorCount);
        }
    }
    protected virtual string? CreateOperationDisplayName(RequestContext context, Operation? operation)
    {
        if (operation is null)
        {
            return null;
        }

        var displayName = StringBuilderPool.Get();

        try
        {
            var rootSelectionSet = operation.RootSelectionSet;
            var selectionCount = rootSelectionSet.Selections.Length;

            displayName.Append('{');
            displayName.Append(' ');

            foreach (var selection in rootSelectionSet.Selections[..Math.Min(3, selectionCount)])
            {
                if (displayName.Length > 2)
                {
                    displayName.Append(' ');
                }

                displayName.Append(selection.ResponseName);
            }

            if (rootSelectionSet.Selections.Length > 3)
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
                displayName.Insert(0, name);
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

    private void UpdateRootActivityName(Activity activity, string displayName)
    {
        var current = activity;

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

    public virtual void EnrichParseDocument(RequestContext context, Activity activity)
    {
        activity.DisplayName = "Parse Document";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Parse);

        if (_options.RenameRootActivity)
        {
            UpdateRootActivityName(activity, $"Begin {activity.DisplayName}");
        }

        context.TryGetOperation(out var operation);

        if (operation is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[operation.Kind]);

            if (!string.IsNullOrEmpty(operation.Name))
            {
                activity.SetTag(GraphQL.Operation.Name, operation.Name);
            }
        }

        var documentInfo = context.OperationDocumentInfo;
        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);

        if (documentInfo.IsPersisted)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }
    }

    public virtual void EnrichRequestError(
        RequestContext context,
        Activity activity,
        System.Exception error)
        => EnrichError(ErrorBuilder.FromException(error).Build(), activity);

    public virtual void EnrichRequestError(
        RequestContext context,
        Activity activity,
        IError error)
        => EnrichError(error, activity);

    public virtual void EnrichValidateDocument(RequestContext context, Activity activity)
    {
        activity.DisplayName = "Validate Document";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Validate);

        if (_options.RenameRootActivity)
        {
            UpdateRootActivityName(activity, $"Begin {activity.DisplayName}");
        }

        context.TryGetOperation(out var operation);

        if (operation is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[operation.Kind]);

            if (!string.IsNullOrEmpty(operation.Name))
            {
                activity.SetTag(GraphQL.Operation.Name, operation.Name);
            }
        }

        var documentInfo = context.OperationDocumentInfo;
        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);

        if (documentInfo.IsPersisted)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }
    }

    public virtual void EnrichValidationError(
        RequestContext context,
        Activity activity,
        IError error)
        => EnrichError(error, activity);

    public virtual void EnrichAnalyzeOperationComplexity(RequestContext context, Activity activity)
    {
        activity.DisplayName = "Analyze Operation Complexity";
    }

    public virtual void EnrichCoerceVariables(RequestContext context, Activity activity)
    {
        activity.DisplayName = "Coerce Variable";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.VariableCoercion);

        context.TryGetOperation(out var operation);

        if (operation is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[operation.Kind]);

            if (!string.IsNullOrEmpty(operation.Name))
            {
                activity.SetTag(GraphQL.Operation.Name, operation.Name);
            }
        }

        var documentInfo = context.OperationDocumentInfo;
        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);

        if (documentInfo.IsPersisted)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }
    }

    public virtual void EnrichCompileOperation(RequestContext context, Activity activity)
    {
        activity.DisplayName = "Compile Operation";
    }

    public virtual void EnrichExecuteOperation(RequestContext context, Activity activity)
    {
        context.TryGetOperation(out var operation);
        activity.DisplayName =
            operation?.Name is { } op
                ? $"Execute Operation {op}"
                : "Execute Operation";

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Execute);

        if (operation is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[operation.Kind]);

            if (!string.IsNullOrEmpty(operation.Name))
            {
                activity.SetTag(GraphQL.Operation.Name, operation.Name);
            }
        }

        var documentInfo = context.OperationDocumentInfo;
        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);

        if (documentInfo.IsPersisted)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }
    }

    public virtual void EnrichResolveFieldValue(IMiddlewareContext context, Activity activity)
    {
        string path;
        string hierarchy;
        BuildPath();

        var selection = context.Selection;
        var coordinate = selection.Field.Coordinate;

        activity.DisplayName = path;
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Resolve);
        activity.SetTag(GraphQL.Selection.Name, selection.ResponseName);
        activity.SetTag(GraphQL.Selection.Field.Type, selection.Field.Type.Print());
        activity.SetTag(GraphQL.Selection.Path, path);
        activity.SetTag(GraphQL.Selection.Hierarchy, hierarchy);
        activity.SetTag(GraphQL.Selection.Field.Name, coordinate.MemberName);
        activity.SetTag(GraphQL.Selection.Field.Coordinate, coordinate.ToString());
        activity.SetTag(GraphQL.Selection.Field.ParentType, coordinate.Name);
        activity.SetTag(GraphQL.Selection.Field.IsDeprecated, selection.Field.IsDeprecated);

        void BuildPath()
        {
            var p = StringBuilderPool.Get();
            var h = StringBuilderPool.Get();
            var index = StringBuilderPool.Get();

            var current = context.Path;

            do
            {
                if (current is NamePathSegment n)
                {
                    p.Insert(0, '/');
                    h.Insert(0, '/');
                    p.Insert(1, n.Name);
                    h.Insert(1, n.Name);

                    if (index.Length > 0)
                    {
                        p.Insert(1 + n.Name.Length, index);
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
            } while (!current.IsRoot);

            path = p.ToString();
            hierarchy = h.ToString();

            StringBuilderPool.Return(p);
            StringBuilderPool.Return(h);
            StringBuilderPool.Return(index);
        }
    }

    public virtual void EnrichResolverError(
        RequestContext context,
        IError error,
        Activity activity)
        => EnrichError(error, activity);

    public virtual void EnrichResolverError(
        IMiddlewareContext middlewareContext,
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
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.DataLoaderBatch);
        activity.SetTag(GraphQL.DataLoader.Batch.Size, keys.Count);

        if (_options.IncludeDataLoaderKeys)
        {
            var temp = keys.Select(t => t.ToString()).ToArray();
            activity.SetTag(GraphQL.DataLoader.Batch.Keys, temp);
        }
    }

    protected virtual void EnrichError(IError error, Activity activity)
    {
        if (error.Exception is { } exception)
        {
            activity.RecordException(exception);
        }

        var tags = new ActivityTagsCollection
        {
            new(SemanticConventions.Exception.Message, error.Message),
            new(SemanticConventions.Exception.Type, error.Code ?? "GRAPHQL_ERROR")
        };

        if (error.Path is not null)
        {
            tags[GraphQL.Errors.Path] = error.Path.ToString();
        }

        if (error.Locations is { Count: > 0 })
        {
            tags[GraphQL.Errors.Location.Column] = error.Locations[0].Column;
            tags[GraphQL.Errors.Location.Line] = error.Locations[0].Line;
        }

        activity.AddEvent(new ActivityEvent(SemanticConventions.Exception.EventName, default, tags));
    }
}
