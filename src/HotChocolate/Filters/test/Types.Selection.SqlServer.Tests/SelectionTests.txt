using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SelectionTests
        : SelectionTestsBase
        , IClassFixture<SqlServerProvider>
    {
        public SelectionTests(SqlServerProvider provider)
            : base(provider)
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
