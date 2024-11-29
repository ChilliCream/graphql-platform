namespace HotChocolate.Execution.Instrumentation;

public class PathExtensionsTests
{
    [Fact]
    public void GetHashCode_Test()
    {
        var path = Path.Root.Append("hero");
        Assert.NotEqual(0, path.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Root_Test()
    {
        Assert.Equal(0, Path.Root.GetHashCode());
    }

    [Fact]
    public void Path_ToString()
    {
        // arrange
        var path = Path.Root.Append("hero");
        path = path.Append("friends");
        path = path.Append(0);
        path = path.Append("name");

        // act
        var result = path.ToString();

        // assert
        Assert.Equal("/hero/friends[0]/name", result);
    }

    [Fact]
    public void Path_ToList()
    {
        // arrange
        var path = Path.Root.Append("hero");
        path = path.Append("friends");
        path = path.Append(0);
        path = path.Append("name");

        // act
        var result = path.ToList();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Path_Equals_Null()
    {
        // arrange
        var hero = Path.Root.Append("hero");
        Path? friends = null;

        // act
        var areEqual = hero.Equals(friends);

        // assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Path_Equals_False()
    {
        // arrange
        var hero = Path.Root.Append("hero");
        var friends = Path.Root.Append("hero");
        friends = friends.Append("friends");

        // act
        var areEqual = hero.Equals(friends);

        // assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Path_Equals_True()
    {
        // arrange
        var friends1 = Path.Root.Append("hero");
        friends1 = friends1.Append("friends");
        var friends2 = Path.Root.Append("hero");
        friends2 = friends2.Append("friends");

        // act
        var areEqual = friends1.Equals(friends2);

        // assert
        Assert.True(areEqual);
    }
}
