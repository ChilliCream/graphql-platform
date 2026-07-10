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
        => new() { Id = review.AuthorId };

    public static Product? GetProduct([Parent] Review review)
        => new() { Upc = review.ProductUpc };
}
