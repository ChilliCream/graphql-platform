namespace HotChocolate;

public static class ExecutionContextData
{
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
    /// The key for setting a flag the document was saved to the persisted operation storage.
    /// </summary>
    public const string DocumentSaved = "HotChocolate.Execution.DocumentSaved";

    /// <summary>
    /// The key for setting a flag that the execution had document validation errors.
    /// </summary>
    public const string ValidationErrors = "HotChocolate.Execution.ValidationErrors";
}
