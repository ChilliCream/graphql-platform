using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace eShop.Accounts;

[ObjectType<User>]
public static partial class UserNode
{
    [ID]
    public static string GetId([Parent] User user) => user.Id;
}
