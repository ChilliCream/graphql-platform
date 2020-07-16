using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SelectionAttributeTests
        : SelectionAttributeTestsBase
        , IClassFixture<SqlServerProvider>
    {
        public SelectionAttributeTests(SqlServerProvider provider)
            : base(provider)
        {
        }
    }
}
