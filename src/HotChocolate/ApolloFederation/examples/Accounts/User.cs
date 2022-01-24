using HotChocolate;
using HotChocolate.ApolloFederation;

namespace Accounts;

public class User
{
    [Key]
    public string Id { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string Username { get; set; } = default!;

    [ReferenceResolver]
    public static Task<User> GetByIdAsync(
        string id,
        UserRepository userRepository)
        => Task.FromResult(userRepository.GetUserById(id));
}