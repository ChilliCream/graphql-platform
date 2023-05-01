namespace HotChocolate.Fusion.Shared.Accounts;

[GraphQLName("Mutation")]
public class AccountMutation
{
    public User AddUser(
        string name,
        string username,
        DateTime birthdate)
        => new User(int.MaxValue, name, birthdate, username);
}
