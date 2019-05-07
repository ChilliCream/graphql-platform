using HotChocolate.Language;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class OperationDefinitionArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public OperationDefinitionArgumentSourceCodeGeneratorTests()
            : base(new OperationDefinitionArgumentSourceCodeGenerator(),
                typeof(OperationDefinitionNode),
                ArgumentKind.OperationDefinition,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
