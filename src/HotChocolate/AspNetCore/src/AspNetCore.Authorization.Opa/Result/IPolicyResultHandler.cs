using HotChocolate.Resolvers;

namespace HotChocolate.AspNetCore.Authorization;

public interface IPolicyResultHandler
{
    Task<ResponseBase?> HandleAsync(string policyPath, HttpResponseMessage response, IMiddlewareContext context);
}
