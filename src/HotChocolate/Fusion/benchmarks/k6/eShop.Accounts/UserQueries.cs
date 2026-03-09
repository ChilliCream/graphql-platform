using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;

namespace eShop.Accounts;

[QueryType]
public static partial class UserQueries
{
    private static readonly List<User> s_users =
    [
        new() { Id = "1", Name = "Uri Goldshtein", Username = "urigo", Birthday = 1234567890 },
        new() { Id = "2", Name = "Dotan Simha", Username = "dotansimha", Birthday = 1234567890 },
        new() { Id = "3", Name = "Kamil Kisiela", Username = "kamilkisiela", Birthday = 1234567890 },
        new() { Id = "4", Name = "Arda Tanrikulu", Username = "ardatan", Birthday = 1234567890 },
        new() { Id = "5", Name = "Gil Gardosh", Username = "gilgardosh", Birthday = 1234567890 },
        new() { Id = "6", Name = "Laurin Quast", Username = "laurin", Birthday = 1234567890 }
    ];

    public static User? GetMe() => s_users[0];

    [Lookup]
    public static User? GetUser([ID] string id) => s_users.FirstOrDefault(u => u.Id == id);

    public static List<User> GetUsers() => s_users;
}
