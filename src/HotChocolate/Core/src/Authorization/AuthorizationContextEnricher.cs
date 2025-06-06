using HotChocolate.Execution;

namespace HotChocolate.Authorization;

internal sealed class AuthorizationContextEnricher : IRequestContextEnricher
{
    public void Enrich(RequestContext context)
    {
        context
            .EnsureAuthorizationRequestDataExists()
            .TryCreateUserStateIfNotExists();
    }
}
