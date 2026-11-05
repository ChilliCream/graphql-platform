using System.Net;

namespace HotChocolate.AspNetCore.Utilities;

/// <summary>
/// Reads the HTTP status code a middleware requested on the context data of an execution result.
/// </summary>
internal static class RequestedStatusCode
{
    /// <summary>
    /// Gets the status code stored under <see cref="ExecutionContextData.HttpStatusCode"/>,
    /// whether it is an <see cref="HttpStatusCode"/> or an <see cref="int"/>.
    /// Returns <c>false</c> if the entry is missing or has another type.
    /// </summary>
    public static bool TryGet(
        IReadOnlyDictionary<string, object?> contextData,
        out HttpStatusCode statusCode)
    {
        if (contextData.TryGetValue(ExecutionContextData.HttpStatusCode, out var value))
        {
            switch (value)
            {
                case HttpStatusCode requested:
                    statusCode = requested;
                    return true;

                case int requestedInt:
                    statusCode = (HttpStatusCode)requestedInt;
                    return true;
            }
        }

        statusCode = default;
        return false;
    }
}
