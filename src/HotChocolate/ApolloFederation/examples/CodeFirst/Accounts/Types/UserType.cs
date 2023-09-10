using HotChocolate.Types;

namespace Accounts;

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(t => ResolveUserById(default!, default!));
    }

    private static Task<User> ResolveUserById(
        string id,
        UserRepository userRepository)
        => userRepository.GetUserByIdAsync(id);
}
