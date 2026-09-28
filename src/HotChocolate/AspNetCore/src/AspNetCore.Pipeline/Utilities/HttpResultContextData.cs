namespace HotChocolate.AspNetCore.Utilities;

/// <summary>
/// The keys the HTTP transport stores on the context data of an execution result.
/// </summary>
internal static class HttpResultContextData
{
    /// <summary>
    /// Marks the result of a request the server read but that is not a well-formed
    /// GraphQL over HTTP request: a body that is not a request object, a batch without a
    /// request object, a parameter of the wrong type, a request parameter that is not valid
    /// JSON, or a request that names neither a document nor a document ID.
    /// </summary>
    public const string RequestNotWellFormed = "HotChocolate.AspNetCore.RequestNotWellFormed";
}
