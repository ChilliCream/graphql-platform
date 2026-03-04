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

        activity.SetTag(GraphQL.Http.Kind, kind);

        if (!(context.Items.TryGetValue(SchemaName, out var value)
            && value is string schemaName))
        {
            schemaName = ISchemaDefinition.DefaultName;
        }

        activity.SetTag(GraphQL.Schema.Name, schemaName);
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

    public virtual void EnrichOperationCost(
        RequestContext context,
        Activity activity,
        double fieldCost,
        double typeCost)
    {
        var documentInfo = context.OperationDocumentInfo;
        activity.SetTag(GraphQL.Document.Hash, FormatDocumentHash(documentInfo.Hash));
        activity.SetTag(GraphQL.Operation.FieldCost, fieldCost);
        activity.SetTag(GraphQL.Operation.TypeCost, typeCost);
    }

    protected void EnrichExecuteRequestCore(
        RequestContext context,
        Activity activity,
        string operationDisplayName,
        OperationType operationType,
        string? operationName)
    {
        activity.DisplayName = operationDisplayName;

        var documentInfo = context.OperationDocumentInfo;
        activity.SetTag(GraphQL.Document.Hash, FormatDocumentHash(documentInfo.Hash));

        if (documentInfo.IsPersisted)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }

        activity.SetTag(GraphQL.Operation.Type, GraphQL.Operation.TypeValues[operationType]);

        if (!string.IsNullOrEmpty(operationName))
        {
            activity.SetTag(GraphQL.Operation.Name, operationName);
        }

        if (options.IncludeDocument && documentInfo.Document is not null)
        {
            activity.SetTag(GraphQL.Document.Body, documentInfo.Document.Print());
        }
    }

    protected void EnrichParseDocumentCore(
        Activity activity,
        OperationDefinitionNode? operationDefinition,
        OperationDocumentInfo documentInfo)
    {
        activity.DisplayName = "GraphQL Document Parsing";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Parse);

        EnrichWithTags(activity, operationDefinition, documentInfo);
    }

    protected void EnrichValidateDocumentCore(
        Activity activity,
        OperationDefinitionNode? operationDefinition,
        OperationDocumentInfo documentInfo)
    {
        activity.DisplayName = "GraphQL Document Validation";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Validate);

        EnrichWithTags(activity, operationDefinition, documentInfo);
    }

    protected void EnrichCoerceVariablesCore(
        Activity activity,
        OperationDefinitionNode? operationDefinition,
        OperationDocumentInfo documentInfo)
    {
        activity.DisplayName = "GraphQL Variable Coercion";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.VariableCoercion);

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

        activity.SetTag(GraphQL.Document.Hash, FormatDocumentHash(documentInfo.Hash));

        if (documentInfo.IsPersisted)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }
    }

    protected string GetOperationDisplayName(
        OperationType operationType,
        string? operationName)
    {
        var operationTypeName = GraphQL.Operation.TypeValues[operationType];
        if (!string.IsNullOrEmpty(operationName))
        {
            return $"{operationTypeName} {operationName}";
        }

        return operationTypeName;
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
            new(SemanticConventions.Exception.Type, error.Code ?? "GRAPHQL_ERROR"),
            new(GraphQL.Errors.Message, error.Message)
        };

        if (error.Path is not null)
        {
            tags[GraphQL.Errors.Path] = FormatPath(error.Path);
        }

        if (error.Locations is { Count: > 0 })
        {
            var locations = new object[error.Locations.Count];
            for (var i = 0; i < error.Locations.Count; i++)
            {
                var location = error.Locations[i];
                locations[i] = new Dictionary<string, object>
                {
                    ["line"] = location.Line,
                    ["column"] = location.Column
                };
            }

            tags[GraphQL.Errors.Locations] = locations;
        }

        activity.AddEvent(new ActivityEvent(SemanticConventions.Exception.EventName, default, tags));
    }

    // TODO: Get rid of this
    protected internal static string FormatDocumentHash(OperationDocumentHash hash)
    {
        if (hash.IsEmpty || string.IsNullOrEmpty(hash.AlgorithmName))
        {
            return hash.Value;
        }

        var algorithm = hash.AlgorithmName;

        if (algorithm.EndsWith("Hash", System.StringComparison.OrdinalIgnoreCase))
        {
            algorithm = algorithm[..^4];
        }

        algorithm = algorithm.ToLowerInvariant();

        if (algorithm == "sha256")
        {
            // TODO: wtf
            algorithm = "sha25";
        }

        return $"{algorithm}:{hash.Value}";
    }

    protected static string? FormatPath(Path? path)
    {
        if (path is null || path.IsRoot)
        {
            return null;
        }

        var segments = path.ToList();
        if (segments.Count == 0)
        {
            return null;
        }

        var result = new StringBuilder();
        foreach (var segment in segments)
        {
            if (segment is string name)
            {
                if (result.Length > 0)
                {
                    result.Append('.');
                }

                result.Append(name);
                continue;
            }

            if (segment is int index)
            {
                result.Append('[');
                result.Append(index);
                result.Append(']');
            }
        }

        return result.ToString();
    }
}
