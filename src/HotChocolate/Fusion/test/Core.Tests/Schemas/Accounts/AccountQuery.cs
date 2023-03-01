namespace HotChocolate.Fusion.Schemas.Accounts;

[GraphQLName("Query")]
public class AccountQuery
{
    public IEnumerable<User> GetUsers([Service] UserRepository repository) =>
        repository.GetUsers();

    public User GetUser(int id, [Service] UserRepository repository) =>
        repository.GetUser(id);
}
