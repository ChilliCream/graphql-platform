namespace Accounts;

public class Query
{
    public User Me(UserRepository userRepository)
        => userRepository.GetUserById("1");
}
