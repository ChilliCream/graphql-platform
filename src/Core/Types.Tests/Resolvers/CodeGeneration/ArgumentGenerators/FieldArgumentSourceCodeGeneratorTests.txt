using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public class FieldArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public FieldArgumentSourceCodeGeneratorTests()
            : base(new FieldArgumentSourceCodeGenerator(),
                typeof(ObjectField),
                ArgumentKind.Field,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
