using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultAttributeTests
        : SingleOrDefaultAttributeTestsBase
        , IClassFixture<InMemoryProvider>
    {
        public SingleOrDefaultAttributeTests(InMemoryProvider provider) : base(provider)
        {
        }
    }
}
