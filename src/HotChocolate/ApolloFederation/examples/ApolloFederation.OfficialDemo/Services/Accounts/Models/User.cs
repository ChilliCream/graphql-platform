using System.Linq;
using System.Threading.Tasks;
using Accounts.Data;
using HotChocolate;
using HotChocolate.ApolloFederation;
using HotChocolate.Language;

namespace Accounts.Models;

[ReferenceResolver(EntityResolverType = typeof(UserReferenceResolver))]
public class User
{
    [Key]
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Username { get; set; } = default!;
}

public static class UserReferenceResolver
{
    public static Task<User> GetUserReferenceResolverAsync(
        [LocalState] ObjectValueNode data,
        [Service] UserRepository userRepository)
    {
        // some code ....
        return Task.FromResult(userRepository.GetUserById(
            (string)data.Fields.First(field => field.Name.Value.Equals("id")).Value.Value!));
    }
}
