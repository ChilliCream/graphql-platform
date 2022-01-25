namespace Reviews;

public class UserRepository
{
    private readonly Dictionary<string, User> _users;

    public UserRepository()
        => _users = CreateUsers().ToDictionary(t => t.Id);

    public Task<User> GetUserById(string id) 
        => Task.FromResult(_users[id]);

    private static IEnumerable<User> CreateUsers()
    {
        yield return new User("1", "@ada");
        yield return new User("2", "@complete");
    }
}
