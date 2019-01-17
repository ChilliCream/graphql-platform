using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public static class ErrorExtensions
    {
        public static IError WithSyntaxNodes(
            this IError error,
            params ISyntaxNode[] nodes)
        {
            return new QueryError(
                error.Message,
                error.Path,
                QueryError.CreateLocations(nodes),
                error.Extensions?.ToImmutableDictionary());
        }
    }
}
