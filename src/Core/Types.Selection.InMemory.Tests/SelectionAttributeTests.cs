using Xunit;

namespace HotChocolate.Types.Selection
{
    public class SelectionAttributeTests
        : SelectionAttributeTestsBase, IClassFixture<InMemoryProvider>
    {
        public SelectionAttributeTests(InMemoryProvider provider) : base(provider)
        {
        }
    }
}
