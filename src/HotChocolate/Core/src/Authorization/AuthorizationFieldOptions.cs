namespace HotChocolate.Authorization;

internal sealed record AuthorizationFieldOptions
{
    public bool AuthorizeAtRequestLevel { get; init; }

    public bool AllowAnonymous { get; init; }
}
