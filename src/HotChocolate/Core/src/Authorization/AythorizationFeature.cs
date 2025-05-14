
using System.Security.Cryptography.X509Certificates;

namespace HotChocolate.Authorization;

public sealed record AuthorizationFeature(
    bool AreDirectivesRegistered);

public sealed record AuthorizationFieldOptions
{
    public bool AuthorizeAtRequestLevel { get; init; }

    public bool AllowAnonymous { get; init; }
}
