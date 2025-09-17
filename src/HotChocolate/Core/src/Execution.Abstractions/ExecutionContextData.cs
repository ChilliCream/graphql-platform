namespace HotChocolate;

public static class ExecutionContextData
{
    /// <summary>
    /// The key to set the flag that the cost should be reported in the response.
    /// </summary>
    public const string ReportCost = "HotChocolate.CostAnalysis.ReportCost";

    /// <summary>
    /// The key to set the flag that only the cost should be validated and the request should not be executed.
    /// </summary>
    public const string ValidateCost = "HotChocolate.CostAnalysis.ValidateCost";

    /// <summary>
    /// The key to determine whether the request is a warmup request.
    /// </summary>
    public const string IsWarmupRequest = "HotChocolate.AspNetCore.Warmup.IsWarmupRequest";

    /// <summary>
    /// The key allows users to override the status code behavior of the default
    /// HTTP response formatter.
    /// </summary>
    public const string HttpStatusCode = "HotChocolate.Execution.Transport.HttpStatusCode";

    /// <summary>
    /// The key for setting a flag that an operation was not allowed during request execution.
    /// </summary>
    public const string OperationNotAllowed = "HotChocolate.Execution.OperationNotAllowed";

    /// <summary>
    /// The key for setting a flag the document was saved to the persisted operation storage.
    /// </summary>
    public const string DocumentSaved = "HotChocolate.Execution.DocumentSaved";

    /// <summary>
    /// The key for setting a flag that the execution had document validation errors.
    /// </summary>
    public const string ValidationErrors = "HotChocolate.Execution.ValidationErrors";

    /// <summary>
    /// Includes the operation plan in the response.
    /// </summary>
    public const string IncludeOperationPlan = "HotChocolate.Execution.EmitOperationPlan";

    /// <summary>
    /// The key to get the user provided transport operation session id when executing
    /// GraphQL over Websocket.
    /// </summary>
    public const string OperationSessionId = "HotChocolate.Execution.Transport.OperationSessionId";

    /// <summary>
    /// The key to retrieve the cache constraints from the operation.
    /// </summary>
    public const string CacheControlConstraints = "HotChocolate.Caching.CacheControlConstraints";

    /// <summary>
    /// The key to get the Cache-Control header value from the context data.
    /// </summary>
    public const string CacheControlHeaderValue = "HotChocolate.Caching.CacheControlHeaderValue";

    /// <summary>
    /// The key to get the Vary header value from the context data.
    /// </summary>
    public const string VaryHeaderValue = "HotChocolate.Caching.VaryHeaderValue";
}
