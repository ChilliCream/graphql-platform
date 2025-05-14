using System.Collections.Immutable;

namespace HotChocolate.Authorization;

public class AuthorizationRequestInfo
{
    public IAuthorizationHandler? Handler { get; set; }

    public ImmutableArray<AuthorizeDirective> Directives { get; set; } = [];
}
