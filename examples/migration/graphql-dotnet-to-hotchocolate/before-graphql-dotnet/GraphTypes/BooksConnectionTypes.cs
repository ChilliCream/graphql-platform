using BeforeGraphQLDotNet.Models;
using GraphQL.Types.Relay;

namespace BeforeGraphQLDotNet.GraphTypes;

// Custom edge and connection graph types so the schema exposes the names
// "BooksEdge" and "BooksConnection" (the default builder would derive
// "BookEdge" / "BookConnection" from the node type name).
public sealed class BooksEdgeType : EdgeType<BookType>
{
    public BooksEdgeType()
    {
        Name = "BooksEdge";
    }
}

public sealed class BooksConnectionType : ConnectionType<BookType, BooksEdgeType>
{
    public BooksConnectionType()
    {
        Name = "BooksConnection";
    }
}
