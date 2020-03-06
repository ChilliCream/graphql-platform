using Xunit;

namespace HotChocolate.Types.Selection
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
