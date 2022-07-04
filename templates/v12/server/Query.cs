namespace HotChocolate.Template.Server;

public class Query
{
    public Book GetBook()
        => new Book("C# in depth.", new Author("Jon Skeet"));
}

public record Book(string Title, Author Author);

public record Author(string Name);
