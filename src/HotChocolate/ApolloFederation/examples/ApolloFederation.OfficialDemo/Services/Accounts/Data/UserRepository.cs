using System.Collections.Generic;
using System.Linq;
using Accounts.Models;

namespace Accounts.Data
{
    public class UserRepository
    {
        private Dictionary<string, User> _users;


        public UserRepository()
        {
            _users = CreateUsers().ToDictionary(t => t.Id);
        }

        public User GetUserById(string id)
        {
            return _users[id];
        }

        private static IEnumerable<User> CreateUsers()
        {
            yield return new User
            {
                Id = "1",
                Name = "Ada Lovelace",
                Username = "@ada",
            };

            yield return new User
            {
                Id = "2",
                Name = "Alan Turing",
                Username = "@complete",
            };
        }
    }
}
