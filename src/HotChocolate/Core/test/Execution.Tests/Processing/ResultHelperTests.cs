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
            var helper = new ResultHelper(CreatePool());
            ResultMap map = helper.RentResultMap(1);
            map.SetValue(0, "abc", "def", false);
            helper.SetData(map);

            // act
            IQueryResult result = helper.BuildResult();

            // assert
            result.ToJson().MatchSnapshot();
        }


        private ResultPool CreatePool()
        {
            return new ResultPool(
                new ResultMapPool(16),
                new ResultMapListPool(16),
                new ResultListPool(16));
        }
    }
}
