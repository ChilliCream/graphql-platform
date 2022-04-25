using Demo.Models;

namespace Demo;
public class Query
{
    [UseFiltering]
    [UseSorting]
    public Book GetBook() =>
        new Book("C# in depth.", new BookInformation(new Author("Jon Skeet")));
}
