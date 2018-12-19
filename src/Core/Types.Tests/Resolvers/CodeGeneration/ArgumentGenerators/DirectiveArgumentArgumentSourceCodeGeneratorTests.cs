using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class DirectiveArgumentArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public DirectiveArgumentArgumentSourceCodeGeneratorTests()
            : base(new DirectiveArgumentArgumentSourceCodeGenerator(),
                typeof(string),
                ArgumentKind.DirectiveArgument,
                ArgumentKind.DataLoader)
        {
        }
    }
}
