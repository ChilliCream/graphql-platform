namespace HotChocolate.AspNetCore.Authorization;

public interface IOpaService
{
    Task<ResponseBase?> QueryAsync(string policyPath, QueryRequest request, CancellationToken token);
}
