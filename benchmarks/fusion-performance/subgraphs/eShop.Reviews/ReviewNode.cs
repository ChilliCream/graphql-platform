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
        => UserQueries.Users.TryGetValue(review.AuthorId, out var user) ? user : null;

    public static Product? GetProduct([Parent] Review review)
        => ProductQueries.Products.TryGetValue(review.ProductUpc, out var product) ? product : null;
}
