namespace HotChocolate.Types;

public class AddressType : ObjectType<AuthorAddress>
{
    protected override void Configure(IObjectTypeDescriptor<AuthorAddress> descriptor)
    {
        descriptor.Name("Address");
    }

    [Query]
    public static async Task<AuthorAddress?> GetAddressByIdAsync(
        int id,
        AuthorAddressRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetAuthorAddressAsync(id, cancellationToken);
}
