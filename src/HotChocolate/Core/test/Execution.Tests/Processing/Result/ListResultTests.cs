namespace HotChocolate.Execution.Processing;

public class ListResultTests
{
    [Fact]
    public void Enumerate_Should_OnlyEnumerateOverSetItem()
    {
        // arrange
        var list = new ListResult();

        // act
        list.EnsureCapacity(10);
        list.AddUnsafe(1);
        list.AddUnsafe(2);
        list.AddUnsafe(3);

        // assert
        using var enumerator = list.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(2, enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(3, enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }
}
