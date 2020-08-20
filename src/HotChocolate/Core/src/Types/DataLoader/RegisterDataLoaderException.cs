#nullable enable

namespace HotChocolate.DataLoader
{
    public class RegisterDataLoaderException 
        : GraphQLException
    {
        public RegisterDataLoaderException(string message) 
            : base(message)
        {
        }
    }
}
