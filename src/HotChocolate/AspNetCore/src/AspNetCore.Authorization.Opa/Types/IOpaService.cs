namespace HotChocolate.AspNetCore.Authorization;

public interface IOpaService
{
    Task<HttpResponseMessage?> QueryAsync(string policyPath, QueryRequest request, CancellationToken token);
}
