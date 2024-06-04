using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types;

public static class BookOperations
{
    [Query]
    [UsePaging]
    public static async Task<IEnumerable<Book>> GetBooksAsync(
        BookRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetBooksAsync(cancellationToken);

    [Query]
    public static async Task<Book?> GetBookByIdAsync(
        int id,
        BookRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetBookAsync(id, cancellationToken);

    [Mutation]
    public static async Task<Book> CreateBookAsync(
        string title,
        int authorId,
        BookRepository repository,
        CancellationToken cancellationToken)
        => await repository.CreateBookAsync(title, authorId, cancellationToken);
}
