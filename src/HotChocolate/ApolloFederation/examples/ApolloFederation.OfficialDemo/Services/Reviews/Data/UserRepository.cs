using System.Collections.Generic;
using System.Linq;
using Reviews.Models;

namespace Reviews.Data
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
                Username = "@ada",
            };

            yield return new User
            {
                Id = "2",
                Username = "@complete",
            };
        }
    }
}
