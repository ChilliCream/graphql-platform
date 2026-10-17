using HotChocolate.Types;

namespace eShop.Accounts;

public class Query
{
    internal static readonly List<User> Users =
    [
        new() { Id = "1", Name = "Uri Goldshtein", Username = "urigo", Birthday = 1234567890 },
        new() { Id = "2", Name = "Dotan Simha", Username = "dotansimha", Birthday = 1234567890 },
        new() { Id = "3", Name = "Kamil Kisiela", Username = "kamilkisiela", Birthday = 1234567890 },
        new() { Id = "4", Name = "Arda Tanrikulu", Username = "ardatan", Birthday = 1234567890 },
        new() { Id = "5", Name = "Gil Gardosh", Username = "gilgardosh", Birthday = 1234567890 },
        new() { Id = "6", Name = "Laurin Quast", Username = "laurin", Birthday = 1234567890 }
    ];

    public User? GetMe() => Users[0];

    public User? GetUser([GraphQLType(typeof(IdType))] string id)
        => Users.FirstOrDefault(u => u.Id == id);

    public List<User> GetUsers() => Users;
}
