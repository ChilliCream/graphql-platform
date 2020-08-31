using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultTests
        : SingleOrDefaultTestsBase
        , IClassFixture<SqlServerProvider>
    {
        public SingleOrDefaultTests(SqlServerProvider provider)
            : base(provider)
        {
        }

        [Fact(Skip = "Does not work with SQLLite")]
        public override void Execute_Selection_Nested_Single()
        {
        }
    }
}
