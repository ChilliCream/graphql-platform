using System.Threading.Tasks;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Lodash
{
    public class SchemaTests : LodashTestBase
    {
        [Fact]
        public async Task Schema_Should_Match()
        {
            // arrange
            SnapshotFullName fullName = new XunitSnapshotFullNameReader().ReadSnapshotFullName();
            var executor = await CreateExecutor();

            // act
            var schema = executor.Schema.ToString();

            // assert
            schema.MatchSnapshot(new SnapshotFullName("schema.graphql", fullName.FolderPath));
        }
    }
}
