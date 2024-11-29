namespace StrawberryShake.Json;

public class JsonSerializationHelperTests
{
    [Fact]
    public void ReadScalarList()
    {
        var list = JsonSerializationHelper.ReadList("[1, 2, 3, 4]");

        Assert.Collection(
            list,
            item => Assert.Equal(1, Assert.IsType<long>(item)),
            item => Assert.Equal(2, Assert.IsType<long>(item)),
            item => Assert.Equal(3, Assert.IsType<long>(item)),
            item => Assert.Equal(4, Assert.IsType<long>(item)));
    }

    [Fact]
    public void ReadNestedScalarList()
    {
        var list = JsonSerializationHelper.ReadList("[[1, 2, 3, 4]]");

        Assert.Collection(
            list,
            nested => Assert.Collection(
                Assert.IsType<List<object>>(nested),
                item => Assert.Equal(1, Assert.IsType<long>(item)),
                item => Assert.Equal(2, Assert.IsType<long>(item)),
                item => Assert.Equal(3, Assert.IsType<long>(item)),
                item => Assert.Equal(4, Assert.IsType<long>(item))));
    }
}
