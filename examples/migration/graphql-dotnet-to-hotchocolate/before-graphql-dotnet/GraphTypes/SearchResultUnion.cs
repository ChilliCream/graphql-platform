using BeforeGraphQLDotNet.Models;
using GraphQL.Types;

namespace BeforeGraphQLDotNet.GraphTypes;

public sealed class SearchResultUnion : UnionGraphType
{
    public SearchResultUnion()
    {
        Name = "SearchResult";

        Type<BookType>();
        Type<AuthorType>();

        ResolveType = obj =>
        {
            if (obj is Book)
            {
                return PossibleTypes.First(t => t.Name == "Book");
            }

            if (obj is Author)
            {
                return PossibleTypes.First(t => t.Name == "Author");
            }

            return null;
        };
    }
}
