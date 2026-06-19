using AfterHotChocolate.Models;

namespace AfterHotChocolate.GraphQL;

// Extends the Book POCO with the author navigation, resolved through the
// AuthorById batch DataLoader (N+1 fix).
[ObjectType<Book>]
public static partial class BookType
{
    public static async Task<Author> GetAuthor(
        [Parent] Book book,
        IAuthorByIdDataLoader authorById,
        CancellationToken cancellationToken)
        => await authorById.LoadRequiredAsync(book.AuthorId, cancellationToken);
}
