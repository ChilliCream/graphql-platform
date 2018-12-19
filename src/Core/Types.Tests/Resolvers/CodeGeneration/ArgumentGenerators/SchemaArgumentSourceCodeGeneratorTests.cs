using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class SchemaArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public SchemaArgumentSourceCodeGeneratorTests()
            : base(new SchemaArgumentSourceCodeGenerator(),
                typeof(ISchema),
                ArgumentKind.Schema,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
