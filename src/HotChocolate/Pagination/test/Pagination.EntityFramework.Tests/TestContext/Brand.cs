// ReSharper disable CollectionNeverUpdated.Global

using System.ComponentModel.DataAnnotations;

namespace HotChocolate.Data.TestContext;

public class Brand
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    public string? DisplayName { get; set; } = default!;

    public string? AlwaysNull { get; set; }

    public ICollection<Product> Products { get; } = new List<Product>();

    public BrandDetails BrandDetails { get; set; } = default!;
}

public class BrandDetails
{
    public Country Country { get; set; } = default!;
}

public class Country
{
    public string Name { get; set; } = default!;
}
