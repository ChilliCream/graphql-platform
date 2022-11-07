using System.Collections.Generic;

namespace HotChocolate.Stitching.Schemas.Accounts
{
    public class Query
    {
        public IEnumerable<User> GetUsers([Service] UserRepository repository) =>
            repository.GetUsers();

        public User GetUser(int id, [Service] UserRepository repository) => 
            repository.GetUser(id);
    }
}