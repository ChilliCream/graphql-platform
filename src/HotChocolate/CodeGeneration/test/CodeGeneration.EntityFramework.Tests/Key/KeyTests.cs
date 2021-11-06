using Xunit;

namespace HotChocolate.CodeGeneration.EntityFramework.Key
{
    public class KeyTests : SchemaTestBase
    {
        [Fact]
        protected override void Works() => WorksImpl();
    }
}
