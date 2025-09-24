#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

public class GraphQLHttpStreamException : Exception
{
    public GraphQLHttpStreamException(string message) : base(message)
    {
    }
}
