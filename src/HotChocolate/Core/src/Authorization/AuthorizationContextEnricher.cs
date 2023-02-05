using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Authorization;

internal sealed class AuthorizationContextEnricher : IRequestContextEnricher
{
    public void Enrich(IRequestContext context)
    {
        if (!context.ContextData.ContainsKey(AuthorizationHandler))
        {
            var authorizationHandler = context.Services.GetRequiredService<IAuthorizationHandler>();
            context.ContextData.Add(AuthorizationHandler, authorizationHandler);
        }
    }
}
