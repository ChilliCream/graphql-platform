namespace HotChocolate.Fusion.Shared.Authors;

public sealed class AuthorRepository
{
    private readonly Dictionary<string, Author> _authors;

    public AuthorRepository()
    {
        _authors = new[]
        {
            new Author("1", "First author", "The first author")
        }.ToDictionary(t => t.Id);
    }

     public IEnumerable<Author> GetAuthors(int limit)
        => _authors.Values.OrderBy(t => t.Id).Take(limit);

    public Author? GetAuthorById(string id)
        => _authors.TryGetValue(id, out var author)
            ? author
            : null;
}
