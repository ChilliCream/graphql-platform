using HotChocolate.Fusion.Suites.AbstractTypes.Products;

namespace HotChocolate.Fusion.Suites.AbstractTypes.Inventory;

public static class InventoryData
{
    public static readonly IReadOnlySet<string> KnownBookIds =
        ProductData.Books.Select(static b => b.Id).ToHashSet(StringComparer.Ordinal);

    public static readonly IReadOnlySet<string> KnownMagazineIds =
        ProductData.Magazines.Select(static m => m.Id).ToHashSet(StringComparer.Ordinal);

    public static DeliveryEstimates? ComputeDelivery(
        string? zip,
        ProductDimension? dimensions)
    {
        if (zip is null || dimensions is null)
        {
            return null;
        }

        if (dimensions.Size == "small" && dimensions.Weight < 1)
        {
            return new DeliveryEstimates
            {
                EstimatedDelivery = "1 day",
                FastestDelivery = "same day"
            };
        }

        if (dimensions.Size == "large" && dimensions.Weight >= 1)
        {
            return new DeliveryEstimates
            {
                EstimatedDelivery = "3 days",
                FastestDelivery = "2 days"
            };
        }

        return null;
    }
}

public sealed class ProductDimension
{
    public string? Size { get; init; }
    public double? Weight { get; init; }
}

public sealed class DeliveryEstimates
{
    public string? EstimatedDelivery { get; init; }
    public string? FastestDelivery { get; init; }
}

public sealed class InventoryBook
{
    public string Id { get; init; } = default!;
    public ProductDimension? Dimensions { get; init; }
}

public sealed class InventoryMagazine
{
    public string Id { get; init; } = default!;
    public ProductDimension? Dimensions { get; init; }
}
