using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;

namespace eShop.Reviews;

[QueryType]
public static partial class UserQueries
{
    [Lookup, Internal]
    public static User? GetUser([ID] string id) => new() { Id = id };
}
