namespace HotChocolate.Types;

[SubscriptionType]
public static partial class Subscription
{
    [Subscribe(With = nameof(SubscribeToOnProductAdded))]
    public static Task<int> OnProductAdded([EventMessage] int productId)
        => Task.FromResult(productId);

    private static async IAsyncEnumerable<int> SubscribeToOnProductAdded(int categoryId)
    {
        await Task.Yield();
        yield return categoryId;
    }

    [Subscribe(With = nameof(SubscribeToOnProductPriceChanged))]
    public static Task<int> OnProductPriceChanged([EventMessage] int newPrice)
        => Task.FromResult(newPrice);

    public static async IAsyncEnumerable<int> SubscribeToOnProductPriceChanged(int productId)
    {
        await Task.Yield();
        yield return productId;
    }
}
