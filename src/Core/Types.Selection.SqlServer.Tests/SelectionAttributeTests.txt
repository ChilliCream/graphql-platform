using Squadron;
using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SelectionAttributeTests
        : SelectionAttributeTestsBase
        , IClassFixture<SqlServerResource>
    {
        public SelectionAttributeTests(SqlServerResource provider)
            : base(new SqlServerProvider(provider))
        {
        }
    }
}
