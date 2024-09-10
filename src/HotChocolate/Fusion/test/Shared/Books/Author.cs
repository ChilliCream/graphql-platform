namespace HotChocolate.Fusion.Shared.Books;

public class Author
{
    public string Id { get; set;}

    public IEnumerable<Book> Books { get; set; }

    public Author(string id, IEnumerable<Book> books) {
        this.Id = id;
        this.Books = books;
    }
}
