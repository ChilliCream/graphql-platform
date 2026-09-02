using System.Text.Json;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The result of a single Nitro GraphQL request. Either the data of the response or the reason
/// why Nitro was unavailable.
/// </summary>
internal sealed class NitroGraphQLResult
{
    private NitroGraphQLResult(JsonElement data, string? failure)
    {
        Data = data;
        Failure = failure;
    }

    /// <summary>
    /// Gets the data of the response.
    /// </summary>
    public JsonElement Data { get; }

    /// <summary>
    /// Gets the reason why Nitro was unavailable, or <c>null</c> when the request succeeded.
    /// </summary>
    public string? Failure { get; }

    public static NitroGraphQLResult Success(JsonElement data)
        => new(data, null);

    public static NitroGraphQLResult Failed(string failure)
        => new(default, failure);
}
