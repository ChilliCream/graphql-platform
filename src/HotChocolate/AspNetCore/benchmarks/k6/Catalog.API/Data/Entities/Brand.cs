// ReSharper disable CollectionNeverUpdated.Global

using System.ComponentModel.DataAnnotations;

namespace eShop.Catalog.Data.Entities;

public sealed class BrandEntity
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    public ICollection<ProductEntity> Products { get; set; } = [];
}
