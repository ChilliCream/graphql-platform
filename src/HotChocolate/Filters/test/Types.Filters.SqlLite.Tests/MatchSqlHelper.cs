using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public static class MatchSqlHelperExtensions
    {
        public static void AssertSnapshot(this MatchSqlHelper sqlHelper)
        {
            Assert.NotNull(sqlHelper);
            Assert.NotNull(sqlHelper.Sql);
            sqlHelper.Sql.MatchSnapshot(new SnapshotNameExtension("_sql"));
        }
    }

    public class MatchSqlHelper
    {
        public string Sql { get; set; }
    }
}
