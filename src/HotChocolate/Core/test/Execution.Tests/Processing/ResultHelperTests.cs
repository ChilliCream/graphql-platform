using HotChocolate.Execution.Processing.Pooling;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Processing
{
    public class ResultHelperTests
    {
        [Fact]
        public void BuildResult_SimpleResultSet_SnapshotMatches()
        {
            // arrange
            var helper = new ResultBuilder(CreatePool());
            ObjectResult map = helper.RentObject(1);
            map.SetValueUnsafe(0, "abc", "def", false);
            helper.SetData(map);

            // act
            IQueryResult result = helper.BuildResult();

            // assert
            result.ToJson().MatchSnapshot();
        }


        private ResultPool CreatePool()
            => new ResultPool(
                new ObjectResultPool(16, 256),
                new ObjectListResultPool(16, 256),
                new ListResultPool(16, 256));
    }
}
