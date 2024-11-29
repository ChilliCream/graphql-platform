namespace HotChocolate.Fusion.Shared.Authors;

[GraphQLName("Query")]
public sealed class AuthorQuery
{
    public Author? AuthorById(
        string id,
        [Service] AuthorRepository repository)
        => repository.GetAuthorById(id);

    public IEnumerable<Author> Authors(int limit, [Service] AuthorRepository repository)
        => repository.GetAuthors(limit);

    public Book BookByAuthorId(
        string authorId,
        [Service] AuthorRepository repository)
    {
        var author = repository.GetAuthorById(authorId);
        return new Book(authorId, author!);
    }
}
