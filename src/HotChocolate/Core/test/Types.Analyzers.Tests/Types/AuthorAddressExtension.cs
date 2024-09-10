namespace HotChocolate.Types;

[ExtendObjectType<Author>]
public static class AuthorAddressExtension
{
    public static Task<AuthorAddress?> GetAddressAsync(
        [Parent] Author author,
        AuthorAddressRepository repository,
        CancellationToken cancellationToken)
        => repository.GetAuthorAddressAsync(author.Id, cancellationToken);
}
