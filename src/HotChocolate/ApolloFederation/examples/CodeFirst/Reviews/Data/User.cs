namespace Reviews;

public class User
{
    public User(string id, string username)
    {
        Id = id;
        Username = username;
    }

    public string Id { get; }

    public string Username { get; }
}
