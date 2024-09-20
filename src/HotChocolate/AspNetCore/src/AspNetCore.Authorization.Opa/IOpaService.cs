namespace HotChocolate.AspNetCore.Authorization;

public interface IOpaService
{
    Task<OpaQueryResponse> QueryAsync(
        string policyPath,
        OpaQueryRequest request,
        CancellationToken ct = default);
}
