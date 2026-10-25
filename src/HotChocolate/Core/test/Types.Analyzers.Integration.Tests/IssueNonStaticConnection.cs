namespace HotChocolate.Types;

public class Author
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}

[ObjectType<Author>]
public static partial class AuthorType
{
    [UsePaging]
    public static IQueryable<Book> Books([Parent(nameof(Author.Id))] Author parent)
        => Enumerable.Empty<Book>().AsQueryable();
}

public class Publisher
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}

[ObjectType<Publisher>]
public static partial class PublisherType
{
    [UsePaging]
    public static IQueryable<Book> Books([Parent(nameof(Publisher.Id))] Publisher parent)
        => Enumerable.Empty<Book>().AsQueryable();
}

[QueryType]
public partial class NonStaticPagedQuery
{
    // Per-instance identifier surfaced through SomeBooks so a test can confirm
    // the resolver method was invoked on a real instance.
    [GraphQLIgnore]
    public string InstanceId { get; } = Guid.NewGuid().ToString("N");

    [UsePaging]
    public IQueryable<Book> SomeBooks() =>
        new[] { new Book { Id = "1", Title = InstanceId } }.AsQueryable();

    [UsePaging]
#pragma warning disable CA1822 // Mark members as static
    public IQueryable<Author> Authors() => Enumerable.Empty<Author>().AsQueryable();

    [UsePaging]
    public IQueryable<Publisher> Publishers() => Enumerable.Empty<Publisher>().AsQueryable();
#pragma warning restore CA1822 // Mark members as static
}
