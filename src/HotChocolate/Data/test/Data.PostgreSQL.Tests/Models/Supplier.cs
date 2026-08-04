using System.ComponentModel.DataAnnotations;

namespace HotChocolate.Data.Models;

public sealed class Supplier
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Website { get; set; }

    public string? ContactEmail { get; set; }

    public ICollection<Brand> Brands { get; set; } = [];
}
