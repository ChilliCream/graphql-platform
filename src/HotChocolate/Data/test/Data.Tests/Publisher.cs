using System.Collections.Generic;

namespace HotChocolate.Data;

public class Publisher
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public string Address { get; set; } = default!;

    public string Zipcode { get; set; } = default!;

    public virtual ICollection<Book> Books { get; set; } = default!;

    public virtual ICollection<Author> Authors { get; set; } = default!;
}
