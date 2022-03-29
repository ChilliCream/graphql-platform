using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.AspNetCore.Authorization;

public interface IPolicyResultHandler
{
    Task<AuthorizeResult> HandleAsync(string policyPath, HttpResponseMessage response, IMiddlewareContext context);
}
