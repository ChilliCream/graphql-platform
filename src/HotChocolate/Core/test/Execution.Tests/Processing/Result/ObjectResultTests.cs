namespace HotChocolate.Execution.Processing;

public class ObjectResultTests
{
    [InlineData(8)]
    [InlineData(4)]
    [InlineData(2)]
    [Theory]
    public void EnsureCapacity(int size)
    {
        // arrange
        var resultMap = new ObjectResult();

        // act
        resultMap.EnsureCapacity(size);

        // assert
        Assert.Equal(size, resultMap.Capacity);
    }

    [Fact]
    public void SetValue()
    {
        // arrange
        var objectResult = new ObjectResult();
        objectResult.EnsureCapacity(1);

        // act
        objectResult.SetValueUnsafe(0, "abc", "def");

        // assert
        Assert.Collection(
            (IEnumerable<ObjectFieldResult>)objectResult,
            t =>
            {
                Assert.Equal("abc", t.Name);
                Assert.Equal("def", t.Value);
            });
    }

    [InlineData(9)]
    [InlineData(8)]
    [InlineData(7)]
    [InlineData(5)]
    [InlineData(4)]
    [InlineData(3)]
    [Theory]
    public void GetValue_ValueIsFound(int capacity)
    {
        // arrange
        var objectResult = new ObjectResult();
        objectResult.EnsureCapacity(capacity);
        objectResult.SetValueUnsafe(0, "abc", "def");
        objectResult.SetValueUnsafe(capacity / 2, "def", "def");
        objectResult.SetValueUnsafe(capacity - 1, "ghi", "def");

        // act
        var value = objectResult.TryGetValue("def", out var index);

        // assert
        Assert.Equal("def", value?.Name);
        Assert.Equal(capacity / 2, index);
    }

    [InlineData(9)]
    [InlineData(8)]
    [InlineData(7)]
    [InlineData(5)]
    [InlineData(4)]
    [InlineData(3)]
    [Theory]
    public void TryGetValue_ValueIsFound(int capacity)
    {
        // arrange
        var objectResult = new ObjectResult();
        objectResult.EnsureCapacity(capacity);
        objectResult.SetValueUnsafe(0, "abc", "def");
        objectResult.SetValueUnsafe(capacity / 2, "def", "def");
        objectResult.SetValueUnsafe(capacity - 1, "ghi", "def");

        IReadOnlyDictionary<string, object?> dict = objectResult;

        // act
        var found = dict.TryGetValue("def", out var value);

        // assert
        Assert.True(found);
        Assert.Equal("def", value);
    }

    [InlineData(9)]
    [InlineData(8)]
    [InlineData(7)]
    [InlineData(5)]
    [InlineData(4)]
    [InlineData(3)]
    [Theory]
    public void TryGetValue_ValueIsNotFound(int capacity)
    {
        // arrange
        var objectResult = new ObjectResult();
        objectResult.EnsureCapacity(capacity);
        objectResult.SetValueUnsafe(0, "abc", "def");
        objectResult.SetValueUnsafe(capacity / 2, "def", "def");
        objectResult.SetValueUnsafe(capacity - 1, "ghi", "def");

        IReadOnlyDictionary<string, object?> dict = objectResult;

        // act
        var found = dict.TryGetValue("jkl", out var value);

        // assert
        Assert.False(found);
        Assert.Null(value);
    }

    [InlineData(9)]
    [InlineData(8)]
    [InlineData(7)]
    [InlineData(5)]
    [InlineData(4)]
    [InlineData(3)]
    [Theory]
    public void ContainsKey(int capacity)
    {
        // arrange
        var objectResult = new ObjectResult();
        objectResult.EnsureCapacity(capacity);
        objectResult.SetValueUnsafe(0, "abc", "def");
        objectResult.SetValueUnsafe(capacity / 2, "def", "def");
        objectResult.SetValueUnsafe(capacity - 1, "ghi", "def");

        IReadOnlyDictionary<string, object?> dict = objectResult;

        // act
        var found = dict.ContainsKey("def");

        // assert
        Assert.True(found);
    }

    [Fact]
    public void EnumerateResultValue()
    {
        // arrange
        var objectResult = new ObjectResult();
        objectResult.EnsureCapacity(5);

        // act
        objectResult.SetValueUnsafe(0, "abc1", "def");
        objectResult.SetValueUnsafe(2, "abc2", "def");
        objectResult.SetValueUnsafe(4, "abc3", "def");

        // assert
        Assert.Collection(
            (IEnumerable<ObjectFieldResult>)objectResult,
            t =>
            {
                Assert.Equal("abc1", t.Name);
                Assert.Equal("def", t.Value);
            },
            t =>
            {
                Assert.Equal("abc2", t.Name);
                Assert.Equal("def", t.Value);
            },
            t =>
            {
                Assert.Equal("abc3", t.Name);
                Assert.Equal("def", t.Value);
            });
    }

    [Fact]
    public void EnumerateKeys()
    {
        // arrange
        var objectResult = new ObjectResult();
        objectResult.EnsureCapacity(5);

        // act
        objectResult.SetValueUnsafe(0, "abc1", "def");
        objectResult.SetValueUnsafe(2, "abc2", "def");
        objectResult.SetValueUnsafe(4, "abc3", "def");

        // assert
        Assert.Collection(
            ((IReadOnlyDictionary<string, object?>)objectResult).Keys,
            t => Assert.Equal("abc1", t),
            t => Assert.Equal("abc2", t),
            t => Assert.Equal("abc3", t));
    }

    [Fact]
    public void EnumerateValues()
    {
        // arrange
        var objectResult = new ObjectResult();
        objectResult.EnsureCapacity(5);

        // act
        objectResult.SetValueUnsafe(0, "abc1", "def");
        objectResult.SetValueUnsafe(2, "abc2", "def");
        objectResult.SetValueUnsafe(4, "abc3", "def");

        // assert
        Assert.Collection(
            ((IReadOnlyDictionary<string, object?>)objectResult).Values,
            t => Assert.Equal("def", t),
            t => Assert.Equal("def", t),
            t => Assert.Equal("def", t));
    }
}
