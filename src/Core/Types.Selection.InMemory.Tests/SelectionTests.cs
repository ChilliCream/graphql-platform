using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SelectionTests
        : SelectionTestsBase
        , IClassFixture<InMemoryProvider>
    {
        public SelectionTests(InMemoryProvider provider) : base(provider)
        {
        }
    }
}
