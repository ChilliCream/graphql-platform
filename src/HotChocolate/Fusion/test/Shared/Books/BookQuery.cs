namespace HotChocolate.Fusion.Shared.Books;

[GraphQLName("Query")]
public sealed class BookQuery
{
    public Book? BookById(
        string id,
        [Service] BookRepository repository)
        => repository.GetBookById(id);

    public IEnumerable<Book> Books(int limit, [Service] BookRepository repository)
        => repository.GetBooks(limit);

    public Author authorById(
        string id,
        [Service] BookRepository repository)
        => new Author(id, repository.GetBooksByAuthorId(id));
}
