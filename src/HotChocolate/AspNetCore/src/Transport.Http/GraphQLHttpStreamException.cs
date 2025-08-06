namespace HotChocolate.Transport.Http;

public class GraphQLHttpStreamException : Exception
{
    public GraphQLHttpStreamException(string message) : base(message)
    {
    }
}
