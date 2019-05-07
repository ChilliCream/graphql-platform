using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class DataLoaderArgumentSourceCodeGeneratorTests
        : ArgumentSourceCodeGeneratorTestBase
    {
        public DataLoaderArgumentSourceCodeGeneratorTests()
            : base(new DataLoaderArgumentSourceCodeGenerator(),
                typeof(string),
                ArgumentKind.DataLoader,
                ArgumentKind.DirectiveArgument)
        {
        }
    }
}
