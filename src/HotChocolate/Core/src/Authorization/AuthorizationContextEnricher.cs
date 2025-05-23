using HotChocolate.Execution;

namespace HotChocolate.Authorization;

internal sealed class AuthorizationContextEnricher : IRequestContextEnricher
{
    public void Enrich(IRequestContext context)
    {
        context
            .EnsureAuthorizationRequestDataExists()
            .TryCreateUserStateIfNotExists();
    }
}
