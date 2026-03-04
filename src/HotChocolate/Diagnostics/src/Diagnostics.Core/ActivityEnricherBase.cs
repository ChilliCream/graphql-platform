using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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
public abstract class ActivityEnricherBase(InstrumentationOptionsBase options)
{
    private readonly ConditionalWeakTable<ISyntaxNode, string> _queryCache = [];

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
            && (options.RequestDetails & RequestDetails.Document) == RequestDetails.Document)
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
                && (options.RequestDetails & RequestDetails.Document) == RequestDetails.Document)
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
            && (options.RequestDetails & RequestDetails.Document) == RequestDetails.Document)
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
        => EnrichError(activity, error);

    public virtual void EnrichHttpRequestError(
        HttpContext context,
        Exception exception,
        Activity activity)
        => activity.RecordException(exception);

    public virtual void EnrichParseHttpRequest(HttpContext context, Activity activity)
    {
        activity.DisplayName = "Parse HTTP Request";
    }

    public virtual void EnrichFormatHttpResponse(HttpContext context, Activity activity)
    {
        activity.DisplayName = "Format HTTP Response";
    }

    public virtual void EnrichParserErrors(HttpContext context, IError error, Activity activity)
        => EnrichError(activity, error);

    public virtual void EnrichRequestError(
        RequestContext context,
        Activity activity,
        Exception exception)
    {
        activity.RecordException(exception);
    }

    public virtual void EnrichRequestError(
        RequestContext context,
        Activity activity,
        IError error)
        => EnrichError(activity, error);

    protected virtual void EnrichError(Activity activity, IError error)
    {
        if (error.Exception is { } exception)
        {
            activity.RecordException(exception);
        }

        var tags = new ActivityTagsCollection
        {
            [GraphQL.Error.Message] = error.Message
        };

        if (error.Path is not null)
        {
            tags[GraphQL.Error.Path] = error.Path.Print();
        }

        if (!string.IsNullOrEmpty(error.Code))
        {
            tags[GraphQL.Error.Code] = error.Code;
        }

        if (error.Locations is { Count: > 0 })
        {
            var locations = new object[error.Locations.Count];
            for (var i = 0; i < error.Locations.Count; i++)
            {
                var location = error.Locations[i];
                locations[i] = new Dictionary<string, int>
                {
                    ["line"] = location.Line,
                    ["column"] = location.Column
                };
            }

            tags[GraphQL.Error.Locations] = locations;
        }

        // TODO: Not sure if this is correct according to the spec
        activity.AddEvent(new ActivityEvent("exception", default, tags));
    }
}
