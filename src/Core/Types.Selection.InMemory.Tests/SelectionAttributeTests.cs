using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SelectionAttributeTests
        : SelectionAttributeTestsBase
        , IClassFixture<InMemoryProvider>
    {
        public SelectionAttributeTests(InMemoryProvider provider) : base(provider)
        {
        }
    }
}
