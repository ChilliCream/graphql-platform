using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class DirectiveArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public DirectiveArgumentSourceCodeGeneratorTests()
            : base(new DirectiveArgumentSourceCodeGenerator(),
                typeof(string),
                ArgumentKind.CustomContext,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
