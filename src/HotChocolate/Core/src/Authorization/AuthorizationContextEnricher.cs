using System.Security.Claims;
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

        if (!context.ContextData.ContainsKey(AuthorizationHandler) &&
            context.ContextData.TryGetValue(nameof(ClaimsPrincipal), out var value) &&
            value is ClaimsPrincipal principal)
        {
            context.ContextData.Add(WellKnownContextData.UserState, new UserState(principal));
        }
    }
}
