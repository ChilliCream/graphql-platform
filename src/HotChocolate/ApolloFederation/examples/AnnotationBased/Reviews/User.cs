using HotChocolate.ApolloFederation;

namespace Reviews;

[ExtendServiceType]
public class User
{
    public User(string id, string username)
    {
        Id = id;
        Username = username;
    }

    [Key]
    [External]
    public string Id { get; }

    [External]
    public string Username { get; }

    public Task<IEnumerable<Review>> GetReviews(
        ReviewRepository repository)
        => repository.GetByUserIdAsync(Id);

    [ReferenceResolver]
    public static Task<User> GetUserByIdAsync(
        string id,
        UserRepository repository)
        => repository.GetUserById(id);
}
