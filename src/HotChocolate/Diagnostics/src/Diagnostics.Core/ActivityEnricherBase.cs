using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Diagnostics;

/// <summary>
/// Base class for activity enrichers that provides shared enrichment logic
/// for HTTP request handling, error handling, and common span enrichment.
/// </summary>
public abstract class ActivityEnricherBase(
    ObjectPool<StringBuilder> stringBuilderPool,
    InstrumentationOptionsBase options)
{
    private readonly ConditionalWeakTable<ISyntaxNode, string> _queryCache = [];

    /// <summary>
    /// Gets the <see cref="StringBuilder"/> pool used by this enricher.
    /// </summary>
    protected ObjectPool<StringBuilder> StringBuilderPool { get; } = stringBuilderPool;

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

        if (options.RenameRootActivity)
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
        activity.SetTag(GraphQL.Http.Request.Type, GraphQL.Http.Request.Types.Single);

        if (request.DocumentId is not null
            && (options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
        {
            activity.SetTag(GraphQL.Http.Request.QueryId, request.DocumentId.Value);
        }

        if (request.DocumentHash is not null
            && (options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
        {
            activity.SetTag(GraphQL.Http.Request.QueryHash, request.DocumentHash.Value);
        }

        if (request.Document is not null
            && (options.RequestDetails & RequestDetails.Query) == RequestDetails.Query)
        {
            if (!_queryCache.TryGetValue(request.Document, out var query))
            {
                query = request.Document.Print();
                _queryCache.Add(request.Document, query);
            }

            activity.SetTag(GraphQL.Http.Request.QueryBody, query);
        }

        if (request.OperationName is not null
            && (options.RequestDetails & RequestDetails.OperationName) == RequestDetails.OperationName)
        {
            activity.SetTag(GraphQL.Http.Request.OperationName, request.OperationName);
        }

        if (request.Variables is not null
            && (options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
        {
            EnrichRequestVariables(context, request, request.Variables, activity);
        }

        if (request.Extensions is not null
            && (options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
        {
            EnrichRequestExtensions(context, request, request.Extensions, activity);
        }
    }

    public virtual void EnrichBatchRequest(
        HttpContext context,
        IReadOnlyList<GraphQLRequest> batch,
        Activity activity)
    {
        activity.SetTag(GraphQL.Http.Request.Type, GraphQL.Http.Request.Types.Batch);

        for (var i = 0; i < batch.Count; i++)
        {
            var request = batch[i];

            if (request.DocumentId is not null
                && (options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
            {
                activity.SetTag(GraphQL.Http.Request.BatchRequest.QueryId(i), request.DocumentId.Value);
            }

            if (request.DocumentHash is not null
                && (options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
            {
                activity.SetTag(GraphQL.Http.Request.BatchRequest.QueryHash(i), request.DocumentHash.Value);
            }

            if (request.Document is not null
                && (options.RequestDetails & RequestDetails.Query) == RequestDetails.Query)
            {
                activity.SetTag(GraphQL.Http.Request.BatchRequest.QueryBody(i), request.Document.Print());
            }

            if (request.OperationName is not null
                && (options.RequestDetails & RequestDetails.OperationName) == RequestDetails.OperationName)
            {
                activity.SetTag(GraphQL.Http.Request.BatchRequest.OperationName(i), request.OperationName);
            }

            if (request.Variables is not null
                && (options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
            {
                EnrichBatchVariables(context, request, request.Variables, i, activity);
            }

            if (request.Extensions is not null
                && (options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
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
        activity.SetTag(GraphQL.Http.Request.Type, GraphQL.Http.Request.Types.OperationBatch);

        if (request.DocumentId is not null
            && (options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
        {
            activity.SetTag(GraphQL.Http.Request.QueryId, request.DocumentId.Value);
        }

        if (request.DocumentHash is not null
            && (options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
        {
            activity.SetTag(GraphQL.Http.Request.QueryHash, request.DocumentHash.Value);
        }

        if (request.Document is not null
            && (options.RequestDetails & RequestDetails.Query) == RequestDetails.Query)
        {
            activity.SetTag(GraphQL.Http.Request.QueryBody, request.Document.Print());
        }

        if (request.OperationName is not null
            && (options.RequestDetails & RequestDetails.OperationName) == RequestDetails.OperationName)
        {
            activity.SetTag(GraphQL.Http.Request.Operations, string.Join(" -> ", operations));
        }

        if (request.Variables is not null
            && (options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
        {
            EnrichRequestVariables(context, request, request.Variables, activity);
        }

        if (request.Extensions is not null
            && (options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
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
        => activity.SetTag(
            GraphQL.Http.Request.BatchRequest.Variables(index),
            variables.RootElement.ToString());

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
                GraphQL.Http.Request.BatchRequest.Extensions(index),
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

        if (options.RenameRootActivity)
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

    public virtual void EnrichValidationError(
        RequestContext context,
        Activity activity,
        IError error)
        => EnrichError(error, activity);

    public virtual void EnrichAnalyzeOperationComplexity(RequestContext context, Activity activity)
    {
        activity.DisplayName = "Analyze Operation Complexity";
    }

    protected void EnrichExecuteRequestCore(
        RequestContext context,
        Activity activity,
        string? operationDisplayName,
        object? operationId,
        OperationType? operationType,
        string? operationName)
    {
        activity.DisplayName = operationDisplayName ?? "Execute Request";

        if (options.RenameRootActivity && operationDisplayName is not null)
        {
            UpdateRootActivityName(activity, operationDisplayName);
        }

        var documentInfo = context.OperationDocumentInfo;
        activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);
        activity.SetTag(GraphQL.Document.Valid, documentInfo.IsValidated);
        activity.SetTag(GraphQL.Operation.Id, operationId);

        if (operationType is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[operationType.Value]);

            if (!string.IsNullOrEmpty(operationName))
            {
                activity.SetTag(GraphQL.Operation.Name, operationName);
            }
        }

        if (options.IncludeDocument && documentInfo.Document is not null)
        {
            activity.SetTag(GraphQL.Document.Body, documentInfo.Document.Print());
        }

        if (context.Result is OperationResult { Errors: [_, ..] errors })
        {
            activity.SetTag(GraphQL.Errors.Count, errors.Count);
        }
    }

    protected void EnrichParseDocumentCore(
        Activity activity,
        OperationDefinitionNode? operationDefinition,
        OperationDocumentInfo documentInfo)
    {
        activity.DisplayName = "Parse Document";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Parse);

        if (options.RenameRootActivity)
        {
            UpdateRootActivityName(activity, $"Begin {activity.DisplayName}");
        }

        EnrichWithTags(activity, operationDefinition, documentInfo);
    }

    protected void EnrichValidateDocumentCore(
        Activity activity,
        OperationDefinitionNode? operationDefinition,
        OperationDocumentInfo documentInfo)
    {
        activity.DisplayName = "Validate Document";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Validate);

        if (options.RenameRootActivity)
        {
            UpdateRootActivityName(activity, $"Begin {activity.DisplayName}");
        }

        EnrichWithTags(activity, operationDefinition, documentInfo);
    }

    protected void EnrichCoerceVariablesCore(
        Activity activity,
        OperationDefinitionNode? operationDefinition,
        OperationDocumentInfo documentInfo)
    {
        activity.DisplayName = "Coerce Variable";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.VariableCoercion);

        // TODO: This is new here. Why do we do this in other places?
        if (options.RenameRootActivity)
        {
            UpdateRootActivityName(activity, $"Begin {activity.DisplayName}");
        }

        EnrichWithTags(activity, operationDefinition, documentInfo);
    }

    protected static void EnrichWithTags(
        Activity activity,
        OperationDefinitionNode? operationDefinition,
        OperationDocumentInfo documentInfo)
    {
        if (operationDefinition is not null)
        {
            activity.SetTag(GraphQL.Operation.Type, GraphQL.Operation.TypeValues[operationDefinition.Operation]);

            var operationName = operationDefinition.Name?.Value;
            if (!string.IsNullOrEmpty(operationName))
            {
                activity.SetTag(GraphQL.Operation.Name, operationName);
            }
        }

        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);

        if (documentInfo.IsPersisted)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }
    }

    protected string BuildOperationDisplayName(
        OperationType operationType,
        string? operationName,
        int selectionCount,
        IEnumerable<string> selectionResponseNames)
    {
        var displayName = StringBuilderPool.Get();

        try
        {
            displayName.Append('{');
            displayName.Append(' ');

            var count = 0;
            foreach (var name in selectionResponseNames)
            {
                if (count >= 3)
                {
                    break;
                }

                if (displayName.Length > 2)
                {
                    displayName.Append(' ');
                }

                displayName.Append(name);
                count++;
            }

            if (selectionCount > 3)
            {
                displayName.Append(' ');
                displayName.Append('.');
                displayName.Append('.');
                displayName.Append('.');
            }

            displayName.Append(' ');
            displayName.Append('}');

            if (operationName is not null)
            {
                displayName.Insert(0, ' ');
                displayName.Insert(0, operationName);
            }

            displayName.Insert(0, ' ');
            displayName.Insert(0, operationType.ToString().ToLowerInvariant());

            return displayName.ToString();
        }
        finally
        {
            StringBuilderPool.Return(displayName);
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
}
