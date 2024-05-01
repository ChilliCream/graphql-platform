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
}

public class Author
{
    public string Name { get; set; } = default!;
}

[ObjectType<Author>]
public static partial class AuthorNode
{
    public static string Address([Parent] Author author) => "something";
}
