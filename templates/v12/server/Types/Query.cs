namespace HotChocolate.Template.Server.Types;

[QueryType]
public static class Query
{
    public static Book GetBook() =>
        new Book
        {
            Title = "C# in depth.",
            Author = new Author
            {
                Name = "Jon Skeet"
            }
        };
}
