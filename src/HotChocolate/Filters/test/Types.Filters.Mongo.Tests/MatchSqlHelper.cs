using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public static class MatchSqlHelperExtensions
    {
        public static void AssertSnapshot(
            this MatchSqlHelper queryHelper,
            string queryNameExtension = "query")
        {
            Assert.NotNull(queryHelper);
            Assert.NotNull(queryHelper.Query);
            queryHelper.Query.MatchSnapshot(new SnapshotNameExtension("_" + queryNameExtension));
        }
    }

    public class MatchSqlHelper
    {
        public string Query { get; set; }
    }
}
