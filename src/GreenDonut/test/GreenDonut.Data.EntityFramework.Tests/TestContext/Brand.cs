// ReSharper disable CollectionNeverUpdated.Global

using System.ComponentModel.DataAnnotations;

namespace GreenDonut.Data.TestContext;

public class Brand
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? AlwaysNull { get; set; }

    public ICollection<Product> Products { get; set; } = [];

    public BrandDetails BrandDetails { get; set; } = null!;
}

public class BrandDetails
{
    public Country Country { get; set; } = null!;
}

public class Country
{
    public string Name { get; set; } = null!;
}
