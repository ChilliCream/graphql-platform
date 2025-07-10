namespace HotChocolate.Data;

public class Publisher
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Zipcode { get; set; } = null!;

    public virtual ICollection<Book> Books { get; set; } = null!;

    public virtual ICollection<Author> Authors { get; set; } = null!;
}
