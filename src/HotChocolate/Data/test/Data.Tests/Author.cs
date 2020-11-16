using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotChocolate.Data
{
    public class Author
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public virtual ICollection<Book> Books { get; set; } =
            new List<Book>();
    }
}
