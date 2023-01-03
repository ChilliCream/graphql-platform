using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Authorization;

public interface IOpaService
{
    Task<HttpResponseMessage?> QueryAsync(
        string policyPath,
        QueryRequest request,
        CancellationToken token);
}
