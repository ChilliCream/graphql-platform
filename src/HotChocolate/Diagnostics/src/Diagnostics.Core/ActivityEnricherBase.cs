using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using HotChocolate.AspNetCore.Instrumentation;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Diagnostics;

/// <summary>
/// Base class for activity enrichers that allows adding additional information
/// to the activity spans created by the diagnostics system.
/// </summary>
public abstract class ActivityEnricherBase
{
    public virtual void EnrichExecuteHttpRequest(
        HttpContext httpContext,
        HttpRequestKind kind,
        Activity activity) { }

    public virtual void EnrichSingleRequest(
        HttpContext httpContext,
        GraphQLRequest request,
        Activity activity) { }

    public virtual void EnrichBatchRequest(
        HttpContext httpContext,
        IReadOnlyList<GraphQLRequest> batch,
        Activity activity) { }

    public virtual void EnrichOperationBatchRequest(
        HttpContext httpContext,
        GraphQLRequest request,
        IReadOnlyList<string> operations,
        Activity activity) { }

    public virtual void EnrichHttpRequestError(
        HttpContext httpContext,
        IError error,
        Activity activity) { }

    public virtual void EnrichHttpRequestError(
        HttpContext httpContext,
        Exception exception,
        Activity activity) { }

    public virtual void EnrichParseHttpRequest(
        HttpContext httpContext,
        Activity activity) { }

    public virtual void EnrichParserErrors(
        HttpContext httpContext,
        IReadOnlyList<IError> errors,
        Activity activity) { }

    public virtual void EnrichFormatHttpResponse(
        HttpContext httpContext,
        Activity activity) { }

    public virtual void EnrichExecuteRequest(
        RequestContext context,
        Activity activity) { }

    public virtual void EnrichRequestError(
        RequestContext context,
        Exception exception,
        Activity activity) { }

    public virtual void EnrichRequestError(
        RequestContext context,
        IError error,
        Activity activity) { }

    public virtual void EnrichParseDocument(
        RequestContext context,
        Activity activity) { }

    public virtual void EnrichValidateDocument(
        RequestContext context,
        Activity activity) { }

    public virtual void EnrichValidationErrors(
        RequestContext context,
        IReadOnlyList<IError> errors,
        Activity activity) { }

    public virtual void EnrichAnalyzeOperationCost(
        RequestContext context,
        Activity activity) { }

    public virtual void EnrichOperationCost(
        RequestContext context,
        double fieldCost,
        double typeCost,
        Activity activity) { }

    public virtual void EnrichCoerceVariables(
        RequestContext context,
        Activity activity) { }

    public virtual void EnrichExecuteOperation(
        RequestContext context,
        Activity activity) { }
}
