#nullable enable

namespace HotChocolate.Fetching;

public class RegisterDataLoaderException : GraphQLException
{
    public RegisterDataLoaderException(string message) : base(message)
    {
    }
}
