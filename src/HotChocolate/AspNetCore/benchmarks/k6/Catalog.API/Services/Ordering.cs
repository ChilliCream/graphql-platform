namespace eShop.Catalog.Services;

internal static class Ordering
{
    public static SortDefinition<Product> DefaultOrder(
        SortDefinition<Product> sortDefinition)
        => sortDefinition
            .IfEmpty(p => p.AddAscending(t => t.Name))
            .AddAscending(t => t.Id);
}
