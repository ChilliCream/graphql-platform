using HotChocolate.Types.Relay;

namespace HotChocolate.Types;

[QueryType]
public static class AuthorQueries
{
    [UsePaging]
    public static async Task<IEnumerable<Author>> GetAuthorsAsync(
        AuthorRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetAuthorsAsync(cancellationToken);

    [NodeResolver]
    public static async Task<Author?> GetAuthorByIdAsync(
        int id,
        AuthorRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetAuthorAsync(id, cancellationToken);
}
