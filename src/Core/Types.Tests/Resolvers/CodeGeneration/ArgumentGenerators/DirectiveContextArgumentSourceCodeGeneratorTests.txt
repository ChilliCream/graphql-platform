using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class DirectiveContextArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public DirectiveContextArgumentSourceCodeGeneratorTests()
            : base(new DirectiveContextArgumentSourceCodeGenerator(),
                typeof(string),
                ArgumentKind.DirectiveContext,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
