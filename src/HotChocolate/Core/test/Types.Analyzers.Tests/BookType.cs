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
}
