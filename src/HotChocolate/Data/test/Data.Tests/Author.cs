namespace HotChocolate.Data;

public class Author
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Book> Books { get; set; } = [];

    public virtual ICollection<Publisher> Publishers { get; set; } = [];
}
