using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class CustomContextArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public CustomContextArgumentSourceCodeGeneratorTests()
            : base(new CustomContextArgumentSourceCodeGenerator(),
                typeof(string),
                ArgumentKind.CustomContext,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
