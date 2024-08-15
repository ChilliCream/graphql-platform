namespace HotChocolate.Types;

public sealed class AuthorAddressRepository(AddressByIdDataLoader addressById)
{
    public async Task<AuthorAddress?> GetAuthorAddressAsync(int authorId, CancellationToken cancellationToken)
        => await addressById.LoadAsync(authorId, cancellationToken);
}
