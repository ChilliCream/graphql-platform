using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace eShop.Reviews;

[ObjectType<Review>]
public static partial class ReviewNode
{
    [ID]
    public static string GetId([Parent] Review review)
        => review.Id;

    public static User? GetAuthor([Parent] Review review)
        => UserQueries.s_users.TryGetValue(review.AuthorId, out var user) ? user : null;

    public static Product? GetProduct([Parent] Review review)
        => ProductQueries.s_products.TryGetValue(review.ProductUpc, out var product) ? product : null;
}
