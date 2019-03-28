using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class CustomArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public CustomArgumentSourceCodeGeneratorTests()
            : base(new CustomArgumentSourceCodeGenerator(),
                typeof(string),
                ArgumentKind.Argument,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
