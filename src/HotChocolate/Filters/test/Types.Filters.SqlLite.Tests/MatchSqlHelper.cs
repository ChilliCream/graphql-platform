using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public static class MatchSqlHelperExtensions
    {
        public static void AssertSnapshot(
            this MatchSqlHelper sqlHelper,
            string sqlNameExtension = "sql")
        {
            Assert.NotNull(sqlHelper);
            Assert.NotNull(sqlHelper.Sql);
            sqlHelper.Sql.MatchSnapshot(new SnapshotNameExtension("_" + sqlNameExtension));
        }
    }

    public class MatchSqlHelper
    {
        public string Sql { get; set; }
    }
}
