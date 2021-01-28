using System.Collections.Generic;

namespace HotChocolate.Data
{
    public class Author
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public virtual ICollection<Book> Books { get; set; } =
            new List<Book>();

        public virtual ICollection<Publisher> Publishers { get; set; } =
            new List<Publisher>();
    }
}
