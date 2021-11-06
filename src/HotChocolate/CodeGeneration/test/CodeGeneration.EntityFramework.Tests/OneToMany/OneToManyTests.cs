using Xunit;

namespace HotChocolate.CodeGeneration.EntityFramework.OneToMany
{
    public class OneToManyTests : SchemaTestBase
    {
        [Fact]
        protected override void Works() => WorksImpl();
    }
}
