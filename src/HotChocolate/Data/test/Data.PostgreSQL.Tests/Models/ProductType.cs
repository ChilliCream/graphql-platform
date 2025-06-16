// ReSharper disable CollectionNeverUpdated.Global

using System.ComponentModel.DataAnnotations;

namespace HotChocolate.Data.Models;

public sealed class ProductType
{
    public int Id { get; set; }

    [Required] public string Name { get; set; } = null!;

    public ICollection<Product> Products { get; set; } = [];
}
