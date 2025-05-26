using System.Collections.Immutable;

namespace HotChocolate.Authorization;

internal class AuthorizationRequestContext
{
    public IAuthorizationHandler? Handler { get; set; }

    public ImmutableArray<AuthorizeDirective> Directives { get; set; } = [];
}
