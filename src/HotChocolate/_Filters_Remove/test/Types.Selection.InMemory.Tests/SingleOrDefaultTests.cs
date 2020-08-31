using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultTests
        : SingleOrDefaultTestsBase
        , IClassFixture<InMemoryProvider>
    {
        public SingleOrDefaultTests(InMemoryProvider provider) : base(provider)
        {
        }
    }
}
