using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace eShop.Reviews;

[ObjectType<User>]
public static partial class UserNode
{
    [ID]
    public static string GetId([Parent] User user)
        => user.Id;

    public static IEnumerable<Review> GetReviews([Parent] User user)
        => ReviewRepository.GetByUserId(user.Id);
}
