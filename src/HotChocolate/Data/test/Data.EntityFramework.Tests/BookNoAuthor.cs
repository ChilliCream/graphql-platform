using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotChocolate.Data;

public class BookNoAuthor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? AuthorId { get; set; }

    [Required]
    public string? Title { get; set; }

    public virtual NoAuthor? Author { get; set; }
}
