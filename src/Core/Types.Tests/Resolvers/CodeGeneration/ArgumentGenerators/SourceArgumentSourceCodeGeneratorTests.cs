using HotChocolate.Resolvers.CodeGeneration;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class SourceArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public SourceArgumentSourceCodeGeneratorTests()
            : base(new SourceArgumentSourceCodeGenerator(),
                typeof(IAsyncLifetime),
                ArgumentKind.Source,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
