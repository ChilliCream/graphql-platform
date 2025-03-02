namespace HotChocolate.AspNetCore.Authorization;

/// <summary>
/// The OPA service interface communicating with OPA server.
/// </summary>
public interface IOpaService
{
    /// <summary>
    /// The method used to query OPA PDP decision based on the request input.
    /// </summary>
    /// <param name="policyPath">The string parameter representing path of the evaluating policy.</param>
    /// <param name="request">The instance <see cref="OpaQueryRequest"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns></returns>
    Task<OpaQueryResponse> QueryAsync(
        string policyPath,
        OpaQueryRequest request,
        CancellationToken ct = default);
}
