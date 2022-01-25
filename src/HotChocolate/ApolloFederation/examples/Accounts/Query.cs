namespace Accounts;

public class Query
{
    public Task<User> Me(UserRepository userRepository)
        => userRepository.GetUserByIdAsync("1");
}
