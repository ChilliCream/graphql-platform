namespace HotChocolate.Execution.Processing;

public class ResultBuilderTests
{
    [Fact]
    public void BuildResult_SimpleResultSet_SnapshotMatches()
    {
        // arrange
        var helper = new ResultBuilder(CreatePool());
        var map = helper.RentObject(1);
        map.SetValueUnsafe(0, "abc", "def", false);
        helper.SetData(map);

        // act
        var result = helper.BuildResult();

        // assert
        result.ToJson().MatchSnapshot();
    }

    private ResultPool CreatePool()
    {
        return new ResultPool(
            new ObjectResultPool(16, 16, 16),
            new ListResultPool(16, 16, 16));
    }
}
