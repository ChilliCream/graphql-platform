// ReSharper disable CollectionNeverUpdated.Global
using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.Models;

public sealed class ProductType
{
    public int Id { get; set; }

    [Required] public string Name { get; set; } = default!;
    
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
