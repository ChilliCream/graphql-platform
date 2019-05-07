using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class ContextArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public ContextArgumentSourceCodeGeneratorTests()
            : base(new ContextArgumentSourceCodeGenerator(),
                typeof(IResolverContext),
                ArgumentKind.Context,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
