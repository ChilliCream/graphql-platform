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
        Activity activity,
        HttpContext httpContext,
        HttpRequestKind kind) { }

    public virtual void EnrichStartSingleRequest(
        Activity activity,
        HttpContext httpContext,
        GraphQLRequest request) { }

    public virtual void EnrichStartBatchRequest(
        Activity activity,
        HttpContext httpContext,
        IReadOnlyList<GraphQLRequest> batch) { }

    public virtual void EnrichStartOperationBatchRequest(
        Activity activity,
        HttpContext httpContext,
        GraphQLRequest request,
        IReadOnlyList<string> operations) { }

    public virtual void EnrichHttpRequestError(
        Activity activity,
        HttpContext httpContext,
        IError error) { }

    public virtual void EnrichHttpRequestError(
        Activity activity,
        HttpContext httpContext,
        Exception exception) { }

    public virtual void EnrichParseHttpRequest(
        Activity activity,
        HttpContext httpContext) { }

    public virtual void EnrichParserErrors(
        Activity activity,
        HttpContext httpContext,
        IReadOnlyList<IError> errors) { }

    public virtual void EnrichFormatHttpResponse(
        Activity activity,
        HttpContext httpContext) { }

    public virtual void EnrichExecuteRequest(
        Activity activity,
        RequestContext context) { }

    public virtual void EnrichRequestError(
        Activity activity,
        RequestContext context,
        Exception exception) { }

    public virtual void EnrichRequestError(
        Activity activity,
        RequestContext context,
        IError error) { }

    public virtual void EnrichParseDocument(
        Activity activity,
        RequestContext context) { }

    public virtual void EnrichValidateDocument(
        Activity activity,
        RequestContext context) { }

    public virtual void EnrichValidationErrors(
        Activity activity,
        RequestContext context,
        IReadOnlyList<IError> errors) { }

    public virtual void EnrichAnalyzeOperationCost(
        Activity activity,
        RequestContext context) { }

    public virtual void EnrichOperationCost(
        Activity activity,
        RequestContext context,
        double fieldCost,
        double typeCost) { }

    public virtual void EnrichCoerceVariables(
        Activity activity,
        RequestContext context) { }

    public virtual void EnrichExecuteOperation(
        Activity activity,
        RequestContext context) { }
}
