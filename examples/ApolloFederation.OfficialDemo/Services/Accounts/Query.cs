using Accounts.Data;
using Accounts.Models;
using HotChocolate;

namespace Accounts
{
    public class Query
    {
        public User Me([Service] UserRepository userRepository)
        {
            return userRepository.GetUserById("1");
        }
    }
}
