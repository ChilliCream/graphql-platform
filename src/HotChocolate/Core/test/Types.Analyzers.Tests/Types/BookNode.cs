using HotChocolate.Types.Relay;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Types;

[ObjectType<Book>]
public static partial class BookNode
{
    static partial void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Title);
        descriptor.Field(t => t.Genre);
    }

    [BindMember(nameof(Book.AuthorId))]
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

    public static string IdAndTitle([HotChocolate.Parent] Book book)
        => $"{book.Id}: {book.Title}";

    public static string GetBookUri([HotChocolate.Parent] Book book, HttpContext context, [LocalState] string? foo = null)
        => context.Request.Path + $"/{book.Id}";

    [NodeResolver]
    public static async Task<Book?> GetBookByIdAsync(
        int id,
        BookRepository repository,
        CancellationToken cancellationToken)
        => await repository.GetBookAsync(id, cancellationToken);
}
