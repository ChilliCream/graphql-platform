// ReSharper disable CollectionNeverUpdated.Global

using System.ComponentModel.DataAnnotations;

namespace HotChocolate.Data.TestContext;

public class Brand
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string? DisplayName { get; set; }

    public ICollection<Product> Products { get; } = [];

    public BrandDetails Details { get; set; } = null!;
}

public class BrandDetails
{
    public Country Country { get; set; } = null!;
}

public class Country
{
    public string Name { get; set; } = null!;
}
