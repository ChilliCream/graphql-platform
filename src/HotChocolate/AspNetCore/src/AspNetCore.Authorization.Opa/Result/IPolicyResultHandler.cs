using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;

namespace HotChocolate.AspNetCore.Authorization;

public interface IPolicyResultHandler
{
    Task<AuthorizeResult> HandleAsync(
        string policyPath,
        HttpResponseMessage response,
        AuthorizationContext context,
        CancellationToken cancellationToken);
}
