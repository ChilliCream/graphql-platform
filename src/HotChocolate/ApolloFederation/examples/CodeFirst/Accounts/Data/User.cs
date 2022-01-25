namespace Accounts;

public class User
{
    public User(string id, string name, string username)
    {
        Id = id;
        Name = name;
        Username = username;
    }

    public string Id { get; }

    public string Name { get; }

    public string Username { get; }
}
