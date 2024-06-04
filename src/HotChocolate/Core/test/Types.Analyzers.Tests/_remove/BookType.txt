namespace HotChocolate.Types;

public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Name("SomeBook");
    }
}

public class Book
{
    public string? Title { get; set; }

    public Author? Author => null;

    public Publisher? Publisher => null;
}

public class Author
{
    public string Name { get; set; } = default!;
}

public readonly record struct Publisher(string Name);

[ObjectType<Author>]
public static partial class AuthorNode
{
    public static string Address([Parent] Author author) => "something";
}

[ObjectType<Publisher>]
public static partial class PublisherNode
{
    public static string Company([Parent] Publisher author) => "something";
}
