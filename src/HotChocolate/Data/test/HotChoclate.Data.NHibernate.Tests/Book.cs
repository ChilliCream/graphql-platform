using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotChocolate.Data
{
    public class Book
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int Id { get; set; }

        [Required]
        public virtual int AuthorId { get; set; }

        [Required]
        public virtual string? Title { get; set; }

        public virtual Author? Author { get; set; }
    }
}
