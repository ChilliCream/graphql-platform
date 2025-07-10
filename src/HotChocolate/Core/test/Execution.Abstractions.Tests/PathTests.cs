namespace HotChocolate;

public class PathTests
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

    [Fact]
    public void Null_Is_Last()
    {
        var path = Path.Parse("/foo");
        Assert.True(path.CompareTo(null) < 0);
    }

    [Fact]
    public void Same_Instance_Is_Equal()
    {
        var path = Path.Parse("/foo");
        Assert.Equal(0, path.CompareTo(path));
    }

    [Fact]
    public void Root_Is_First()
    {
        var root = Path.Root;
        var path = Path.Parse("/foo");

        Assert.True(root.CompareTo(path) < 0);
        Assert.True(path.CompareTo(root) > 0);
        Assert.Equal(0, root.CompareTo(root));
    }

    [Fact]
    public void Shorter_Path_Is_First()
    {
        var shorter = Path.Parse("/foo");
        var longer = Path.Parse("/foo/bar");

        Assert.True(shorter.CompareTo(longer) < 0);
        Assert.True(longer.CompareTo(shorter) > 0);
    }

    [Fact]
    public void Lexicographical_Order_By_Names()
    {
        var a = Path.Parse("/a");
        var b = Path.Parse("/b");

        Assert.True(a.CompareTo(b) < 0);
        Assert.True(b.CompareTo(a) > 0);
    }

    [Fact]
    public void Indexers_After_Names()
    {
        var name = Path.Parse("/foo");
        var index = Path.Parse("/foo[0]");

        Assert.True(name.CompareTo(index) < 0);
        Assert.True(index.CompareTo(name) > 0);
    }

    [Fact]
    public void Indexes_Compare_By_Number()
    {
        var index0 = Path.Parse("/foo[0]");
        var index1 = Path.Parse("/foo[1]");

        Assert.True(index0.CompareTo(index1) < 0);
        Assert.True(index1.CompareTo(index0) > 0);
    }

    [Fact]
    public void Complex_Ordering()
    {
        var a = Path.Parse("/bar");
        var b = Path.Parse("/bar[3]");
        var c = Path.Parse("/foo");
        var d = Path.Parse("/foo[0]");
        var e = Path.Parse("/bar[3][2]");
        var f = Path.Parse("/bar[3]/foo");

        var paths = new[] { f, d, c, b, e, a };
        Array.Sort(paths);

        string[] expected =
        [
            "/bar",
            "/bar[3]",
            "/bar[3][2]",
            "/bar[3]/foo",
            "/foo",
            "/foo[0]"
        ];

        for (var i = 0; i < paths.Length; i++)
        {
            Assert.Equal(expected[i], paths[i].Print());
        }
    }
}
