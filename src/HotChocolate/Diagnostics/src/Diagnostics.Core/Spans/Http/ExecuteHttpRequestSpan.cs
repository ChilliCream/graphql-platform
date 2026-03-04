using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Diagnostics;

internal sealed class ExecuteHttpRequestSpan(
    Activity activity,
    HttpContext httpContext,
    HttpRequestKind kind,
    ActivityEnricherBase enricher,
    InstrumentationOptionsBase options) : SpanBase(activity)
{
    public static ExecuteHttpRequestSpan? Start(
        ActivitySource source,
        HttpContext httpContext,
        HttpRequestKind kind,
        ActivityEnricherBase enricher,
        InstrumentationOptionsBase options)
    {
        var activity = source.StartActivity();

        if (activity is null)
        {
            return null;
        }

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

        if (!(httpContext.Items.TryGetValue(SchemaName, out var value)
            && value is string schemaName))
        {
            schemaName = ISchemaDefinition.DefaultName;
        }

        activity.SetTag(GraphQL.Schema.Name, schemaName);
        activity.MarkAsSuccess();

        return new ExecuteHttpRequestSpan(activity, httpContext, kind, enricher, options);
    }

    public void SetSingleRequestDetails(GraphQLRequest request)
    {
        Activity.SetTag(GraphQL.Http.Request.Type, GraphQL.Http.Request.Types.Single);

        if (request.DocumentId is not null
            && (options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
        {
            Activity.SetTag(GraphQL.Http.Request.QueryId, request.DocumentId.Value);
        }

        if (request.DocumentHash is not null
            && (options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
        {
            Activity.SetTag(GraphQL.Http.Request.QueryHash, request.DocumentHash.Value);
        }

        if (request.Document is not null
            && (options.RequestDetails & RequestDetails.Document) == RequestDetails.Document)
        {
            Activity.SetTag(GraphQL.Http.Request.QueryBody, request.Document.Print());
        }

        if (request.OperationName is not null
            && (options.RequestDetails & RequestDetails.OperationName) == RequestDetails.OperationName)
        {
            Activity.SetTag(GraphQL.Http.Request.OperationName, request.OperationName);
        }

        if (request.Variables is not null
            && (options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
        {
            Activity.SetTag(GraphQL.Http.Request.Variables, request.Variables.RootElement.ToString());
        }

        if (request.Extensions is not null
            && (options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
        {
            try
            {
                Activity.SetTag(GraphQL.Http.Request.Extensions, request.Extensions.RootElement.ToString());
            }
            catch
            {
                // Ignore any errors
            }
        }

        enricher.EnrichStartSingleRequest(Activity, httpContext, request);
    }

    public void SetBatchRequestDetails(IReadOnlyList<GraphQLRequest> batch)
    {
        Activity.SetTag(GraphQL.Http.Request.Type, GraphQL.Http.Request.Types.Batch);

        for (var i = 0; i < batch.Count; i++)
        {
            var request = batch[i];

            if (request.DocumentId is not null
                && (options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
            {
                Activity.SetTag(GraphQL.Http.Request.BatchRequest.QueryId(i), request.DocumentId.Value);
            }

            if (request.DocumentHash is not null
                && (options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
            {
                Activity.SetTag(GraphQL.Http.Request.BatchRequest.QueryHash(i), request.DocumentHash.Value);
            }

            if (request.Document is not null
                && (options.RequestDetails & RequestDetails.Document) == RequestDetails.Document)
            {
                Activity.SetTag(GraphQL.Http.Request.BatchRequest.QueryBody(i), request.Document.Print());
            }

            if (request.OperationName is not null
                && (options.RequestDetails & RequestDetails.OperationName) == RequestDetails.OperationName)
            {
                Activity.SetTag(GraphQL.Http.Request.BatchRequest.OperationName(i), request.OperationName);
            }

            if (request.Variables is not null
                && (options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
            {
                Activity.SetTag(
                    GraphQL.Http.Request.BatchRequest.Variables(i),
                    request.Variables.RootElement.ToString());
            }

            if (request.Extensions is not null
                && (options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
            {
                try
                {
                    Activity.SetTag(
                        GraphQL.Http.Request.BatchRequest.Extensions(i),
                        request.Extensions.RootElement.ToString());
                }
                catch
                {
                    // Ignore any errors
                }
            }
        }

        enricher.EnrichStartBatchRequest(Activity, httpContext, batch);
    }

    public void SetOperationBatchRequestDetails(
        GraphQLRequest request,
        IReadOnlyList<string> operations)
    {
        Activity.SetTag(GraphQL.Http.Request.Type, GraphQL.Http.Request.Types.OperationBatch);

        if (request.DocumentId is not null
            && (options.RequestDetails & RequestDetails.Id) == RequestDetails.Id)
        {
            Activity.SetTag(GraphQL.Http.Request.QueryId, request.DocumentId.Value);
        }

        if (request.DocumentHash is not null
            && (options.RequestDetails & RequestDetails.Hash) == RequestDetails.Hash)
        {
            Activity.SetTag(GraphQL.Http.Request.QueryHash, request.DocumentHash.Value);
        }

        if (request.Document is not null
            && (options.RequestDetails & RequestDetails.Document) == RequestDetails.Document)
        {
            Activity.SetTag(GraphQL.Http.Request.QueryBody, request.Document.Print());
        }

        if (request.OperationName is not null
            && (options.RequestDetails & RequestDetails.OperationName) == RequestDetails.OperationName)
        {
            Activity.SetTag(GraphQL.Http.Request.Operations, string.Join(" -> ", operations));
        }

        if (request.Variables is not null
            && (options.RequestDetails & RequestDetails.Variables) == RequestDetails.Variables)
        {
            Activity.SetTag(GraphQL.Http.Request.Variables, request.Variables.RootElement.ToString());
        }

        if (request.Extensions is not null
            && (options.RequestDetails & RequestDetails.Extensions) == RequestDetails.Extensions)
        {
            try
            {
                Activity.SetTag(GraphQL.Http.Request.Extensions, request.Extensions.RootElement.ToString());
            }
            catch
            {
                // Ignore any errors
            }
        }

        enricher.EnrichStartOperationBatchRequest(Activity, httpContext, request, operations);
    }

    protected override void OnComplete()
    {
        enricher.EnrichExecuteHttpRequest(Activity, httpContext, kind);
    }

    public void RecordError(IError error)
    {
        Activity.RecordError(error);
        Activity.MarkAsError();
        enricher.EnrichHttpRequestError(Activity, httpContext, error);
        enricher.EnrichError(Activity, error);
    }

    public void RecordError(Exception exception)
    {
        Activity.RecordException(exception);
        Activity.MarkAsError();
        enricher.EnrichHttpRequestError(Activity, httpContext, exception);
        enricher.EnrichException(Activity, exception);
    }
}
