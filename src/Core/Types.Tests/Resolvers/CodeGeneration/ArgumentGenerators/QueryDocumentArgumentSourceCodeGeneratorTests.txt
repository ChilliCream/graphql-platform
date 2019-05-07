using HotChocolate.Language;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class QueryDocumentArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public QueryDocumentArgumentSourceCodeGeneratorTests()
            : base(new QueryDocumentArgumentSourceCodeGenerator(),
                typeof(DocumentNode),
                ArgumentKind.QueryDocument,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
