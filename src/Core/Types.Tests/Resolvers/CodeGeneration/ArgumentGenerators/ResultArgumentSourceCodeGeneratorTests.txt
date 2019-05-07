using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class ResultArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public ResultArgumentSourceCodeGeneratorTests()
            : base(new ResultArgumentSourceCodeGenerator(),
                typeof(string),
                ArgumentKind.ResolverResult,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
