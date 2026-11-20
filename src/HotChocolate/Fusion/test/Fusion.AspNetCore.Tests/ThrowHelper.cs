namespace HotChocolate.Fusion;

internal static class ThrowHelper
{
    public static GraphQLException VolumeUnavailable(string productId)
        => new(new Error
        {
            Message = $"volume unavailable for product {productId}",
            Extensions = new Dictionary<string, object?>
            {
                ["code"] = "VOLUME_UNAVAILABLE",
                ["productId"] = productId
            }
        });
}
