namespace HotChocolate.Fusion.Suites.SkipEntityFetch.B;

internal static class BData
{
    public static readonly Product Product = new()
    {
        Id = "p1",
        Details = new ProductDetails
        {
            Color = "red",
            Size = "M"
        },
        Inventory = new ProductInventory
        {
            InStock = true,
            Warehouse = "WH-1"
        }
    };
}
