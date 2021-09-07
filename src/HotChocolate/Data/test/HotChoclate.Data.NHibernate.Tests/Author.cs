using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace HotChocolate.Data
{
    public class Author
    {

        public virtual  int Id { get; set; }

        [Required]
        public virtual string? Name { get; set; }

        public virtual ICollection<Book> Books { get; set; } =
            new List<Book>();
    }
}
