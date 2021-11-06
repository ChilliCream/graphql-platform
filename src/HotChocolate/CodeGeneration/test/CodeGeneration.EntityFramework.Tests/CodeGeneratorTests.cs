using Xunit;

namespace HotChocolate.CodeGeneration.EntityFramework
{
    public class CodeGeneratorTests : SchemaTestBase
    {
        [Fact]
        protected override void Works() => WorksImpl();
    }
}
