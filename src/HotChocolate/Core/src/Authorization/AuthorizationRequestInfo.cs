using System.Collections.Immutable;

namespace HotChocolate.Authorization;

public class AuthorizationRequestData
{
    public IAuthorizationHandler? Handler { get; set; }

    public ImmutableArray<AuthorizeDirective> Directives { get; set; } = [];
}
