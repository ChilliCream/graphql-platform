using Squadron;
using Xunit;

namespace HotChocolate.Types.Selection
{
    public class SelectionTests
        : SelectionTestsBase, IClassFixture<SqlServerResource>
    {
        public SelectionTests(SqlServerResource provider)
            : base(new SqlServerProvider(provider))
        {
        }

        public override void Execute_Selection_Array()
        {
            // EF does not support array
            Assert.True(true);
        }

        public override void Execute_Selection_ArrayDeep()
        {
            // EF does not support array
            Assert.True(true);
        }
    }
}
