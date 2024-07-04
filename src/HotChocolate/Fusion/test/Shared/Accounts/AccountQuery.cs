using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Accounts;

[GraphQLName("Query")]
public class AccountQuery
{
    public Viewer Viewer { get; } = new();

    public IEnumerable<User> GetUsers([Service] UserRepository repository) =>
        repository.GetUsers();

    [NodeResolver]
    public User? GetUserById(int id, [Service] UserRepository repository) =>
        repository.GetUser(id);

    public IEnumerable<User> GetUsersById(
        [ID(nameof(User))] int[] ids,
        [Service] UserRepository repository)
    {
        foreach (var id in ids)
        {
            var user = repository.GetUser(id);

            if (user is not null)
            {
                yield return user;
            }
        }
    }

    public string? GetErrorField(IResolverContext context)
    {
        context.ReportError("SOME TOP LEVEL USER ERROR");
        return null;
    }
}
