using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;

namespace eShop.Reviews;

[QueryType]
public static partial class UserQueries
{
    internal static readonly Dictionary<string, User> Users =
        new(StringComparer.Ordinal)
    {
        ["1"] = new() { Id = "1" },
        ["2"] = new() { Id = "2" },
        ["3"] = new() { Id = "3" },
        ["4"] = new() { Id = "4" },
        ["5"] = new() { Id = "5" },
        ["6"] = new() { Id = "6" }
    };

    [Lookup, Internal]
    public static User? GetUser([ID] string id)
        => Users.TryGetValue(id, out var user) ? user : null;
}
