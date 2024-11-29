namespace HotChocolate.Types;

public sealed class AuthorRepository
{
    private readonly Dictionary<int, Author> _authors = new()
    {
        { 1, new Author(1, "Author 1") },
        { 2, new Author(2, "Author 2") },
        { 3, new Author(3, "Author 3") }
    };

    public Task<Author?> GetAuthorAsync(int id, CancellationToken cancellationToken)
    {
        if (_authors.TryGetValue(id, out var author))
        {
            return Task.FromResult<Author?>(author);
        }

        return Task.FromResult<Author?>(null);
    }

    public Task<IEnumerable<Author>> GetAuthorsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<Author>>(_authors.Values.OrderBy(t => t.Id));

    public Task<Author> CreateAuthorAsync(string name, CancellationToken cancellationToken)
    {
        var author = new Author(_authors.Count + 1, name);
        _authors.Add(author.Id, author);
        return Task.FromResult(author);
    }
}
