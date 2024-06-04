using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Relay;

namespace HotChocolate.Types;

[ObjectType<Book>]
public static partial class BookNode
{
    static partial void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Id);
    }

    public static async Task<Author?> GetAuthorAsync(
        [Parent] Book book,
        AuthorRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetAuthorAsync(book.AuthorId, cancellationToken);

    [UsePaging]
    public static async Task<IEnumerable<Chapter>> GetChapterAsync(
        [Parent] Book book,
        ChapterRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetChaptersAsync(book.Id, cancellationToken);

    public static string IdAndTitle([Parent] Book book)
        => $"{book.Id}: {book.Title}";

    [NodeResolver]
    public static async Task<Book?> GetBookByIdAsync(
        int id,
        BookRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetBookAsync(id, cancellationToken);
}
