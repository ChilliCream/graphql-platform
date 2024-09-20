namespace HotChocolate.Execution.Processing;

public class ObjectFieldResultTests
{
    [Fact]
    public void SetNullable()
    {
        // arrange
        var field = new ObjectFieldResult();

        // act
        field.Set("abc", "def", true);

        // assert
        Assert.Equal("abc", field.Name);
        Assert.Equal("def", field.Value);
        Assert.True(field.IsNullable);
        Assert.True(field.IsInitialized);
    }

    [Fact]
    public void SetNonNullable()
    {
        // arrange
        var field = new ObjectFieldResult();

        // act
        field.Set("abc", "def", false);

        // assert
        Assert.Equal("abc", field.Name);
        Assert.Equal("def", field.Value);
        Assert.False(field.IsNullable);
        Assert.True(field.IsInitialized);
    }

    [Fact]
    public void NewInstance()
    {
        // arrange
        // act
        var field = new ObjectFieldResult();

        // assert
        Assert.Null(field.Name);
        Assert.Null(field.Value);
        Assert.True(field.IsNullable);
        Assert.False(field.IsInitialized);
    }

    [Fact]
    public void ResetInstance()
    {
        // arrange
        var field = new ObjectFieldResult();
        field.Set("abc", "def", false);

        // act
        field.Reset();

        // assert
        Assert.Null(field.Name);
        Assert.Null(field.Value);
        Assert.True(field.IsNullable);
        Assert.False(field.IsInitialized);
    }
}
