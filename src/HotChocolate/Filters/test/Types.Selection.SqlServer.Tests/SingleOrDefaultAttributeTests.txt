using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SingleOrDefaultAttributeTests
        : SingleOrDefaultAttributeTestsBase
        , IClassFixture<SqlServerProvider>
    {
        public SingleOrDefaultAttributeTests(SqlServerProvider provider)
            : base(provider)
        {
        }
    }
}
