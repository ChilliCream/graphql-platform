using System.Text.Json;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The result of a single Nitro GraphQL request. Either the data of the response or the reason
/// why Nitro was unavailable.
/// </summary>
internal sealed class NitroGraphQLResult
{
    private NitroGraphQLResult(JsonElement data, string? failure, string? failureCode)
    {
        Data = data;
        Failure = failure;
        FailureCode = failureCode;
    }

    /// <summary>
    /// Gets the data of the response.
    /// </summary>
    public JsonElement Data { get; }

    /// <summary>
    /// Gets the reason why Nitro was unavailable, or <c>null</c> when the request succeeded.
    /// </summary>
    public string? Failure { get; }

    /// <summary>
    /// Gets the error code that Nitro reported for the failure, or <c>null</c> when Nitro
    /// reported no code.
    /// </summary>
    public string? FailureCode { get; }

    public static NitroGraphQLResult Success(JsonElement data)
        => new(data, null, null);

    public static NitroGraphQLResult Failed(string failure)
        => new(default, failure, null);

    public static NitroGraphQLResult Failed(string failure, string? failureCode)
        => new(default, failure, failureCode);
}
