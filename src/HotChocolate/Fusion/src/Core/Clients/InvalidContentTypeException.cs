namespace HotChocolate.Fusion.Clients;

public sealed class InvalidContentTypeException : Exception
{
    public InvalidContentTypeException(string message) : base(message)
    {
    }
}
