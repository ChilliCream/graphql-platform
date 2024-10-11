// ReSharper disable CollectionNeverUpdated.Global

using System.ComponentModel.DataAnnotations;

namespace HotChocolate.Execution.TestContext;

public class Brand
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    [MaxLength(100)]
    public string? DisplayName { get; set; }

    public ICollection<Product> Products { get; } = new List<Product>();

    public BrandDetails Details { get; set; } = default!;
}

public class BrandDetails
{
    public Country Country { get; set; } = default!;
}

public class Country
{
    public string Name { get; set; } = default!;
}
