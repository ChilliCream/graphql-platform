namespace HotChocolate.Fusion.Shared.Accounts;

public class Viewer
{
    public SomeData Data { get; } = new();

    public User? User([Service] UserRepository repository)
        => repository.GetUser(1);
}
