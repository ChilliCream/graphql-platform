using HotChocolate.Language;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class FieldSelectionArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public FieldSelectionArgumentSourceCodeGeneratorTests()
            : base(new FieldSelectionArgumentSourceCodeGenerator(),
                typeof(FieldNode),
                ArgumentKind.FieldSelection,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
