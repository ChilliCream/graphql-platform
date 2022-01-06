using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Accounts.Data;
using HotChocolate;
using HotChocolate.ApolloFederation;
using HotChocolate.Language;

namespace Accounts.Models
{
    [ReferenceResolver(EntityResolverType = typeof(UserReferenceResolver))]
    public class User
    {
        [Key]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
    }

    public static class UserReferenceResolver
    {
        public static async Task<User> GetUserReferenceResolverAsync(
            [LocalState] ObjectValueNode data,
            [Service] UserRepository userRepository)
        {
            // some code ....
            return userRepository.GetUserById((string)data.Fields.First(field => field.Name.Value.Equals("id")).Value.Value);
        }
    }
}
