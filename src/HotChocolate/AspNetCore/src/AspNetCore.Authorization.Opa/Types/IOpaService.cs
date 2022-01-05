namespace HotChocolate.AspNetCore.Authorization;

public interface IOpaService
{
    Task<QueryResponse?> QueryAsync(string policyPath, QueryRequest request, CancellationToken token);
}
