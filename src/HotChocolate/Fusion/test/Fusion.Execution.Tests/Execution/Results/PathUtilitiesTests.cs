namespace HotChocolate.Fusion.Execution.Results;

public class PathUtilitiesTests
{
    [Theory]
    [InlineData("/viewer/a", "/viewer", true, true)]
    [InlineData("/viewer/a", "/viewer", false, true)]
    [InlineData("/node/shared/a", "/node/shared", true, true)]
    [InlineData("/viewer/a", "/other", true, false)]
    [InlineData("/viewer/a", "/viewer/b", true, false)]
    [InlineData("/viewer", "/viewer", true, true)]
    [InlineData("/viewer", "/viewer", false, false)]
    [InlineData("/viewer", "/viewer/a", true, false)]
    [InlineData("/items[0]/shared/a", "/items[0]/shared", true, true)]
    [InlineData("/items[1]/shared/a", "/items[0]/shared", true, false)]
    [InlineData("/items[0]", "/items/name", true, false)]
    [InlineData("/items/name", "/items[0]", true, false)]
    [InlineData("/", "/", true, true)]
    [InlineData("/", "/", false, false)]
    [InlineData("/viewer", "/", false, true)]
    public void IsPathInSubtree_Should_CompareCompletePaths_When_SegmentsAreKnown(
        string pathText,
        string subtreeText,
        bool includeSelf,
        bool expected)
    {
        // arrange
        var path = Path.Parse(pathText);
        var subtree = Path.Parse(subtreeText);

        // act
        var actual = PathUtilities.IsPathInSubtree(path, subtree, includeSelf);

        // assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsPathInSubtree_Should_PreserveNullFailure_When_EitherOperandIsNull(bool nullPath)
    {
        // arrange
        var path = nullPath ? null : Path.Root;
        var subtree = nullPath ? Path.Root : null;

        // act
        var exception = Record.Exception(() => PathUtilities.IsPathInSubtree(path!, subtree!, true));

        // assert
        Assert.IsType<NullReferenceException>(exception);
    }

    [Theory]
    [InlineData("discarded")]
    [InlineData("pathAncestor")]
    [InlineData("subtreeAncestor")]
    [InlineData("bothAncestors")]
    [InlineData("sameReference")]
    [InlineData("customZeroRoot")]
    public void IsPathInSubtree_Should_PreserveUnsupportedFailure_When_AnyAncestorIsCustom(string scenario)
    {
        // arrange
        var custom = new CustomPath();
        var (path, subtree) = scenario switch
        {
            "discarded" => (new CustomPath(Path.Root.Append("viewer")), Path.Root.Append("viewer")),
            "pathAncestor" => (custom.Append("left"), Path.Root.Append("right")),
            "subtreeAncestor" => (Path.Root.Append("left"), custom.Append("right")),
            "bothAncestors" => (custom.Append("left"), custom.Append("right")),
            "sameReference" => (custom.Append("same"), null),
            "customZeroRoot" => (Path.Root, custom),
            _ => throw new NotSupportedException()
        };
        subtree ??= path;

        // act
        var exception = Record.Exception(() => PathUtilities.IsPathInSubtree(path, subtree, true));

        // assert
        Assert.IsType<NotSupportedException>(exception);
    }

    [Fact]
    public void IsPathInSubtree_Should_PreserveEarlyGuards_When_CustomSegmentsWouldOtherwiseThrow()
    {
        // arrange
        var custom = new CustomPath();
        var same = custom.Append("field");

        // act
        var rootIncludingSelf = PathUtilities.IsPathInSubtree(custom, Path.Root, true);
        var rootStrict = PathUtilities.IsPathInSubtree(custom, Path.Root, false);
        var equalLengthStrict = PathUtilities.IsPathInSubtree(same, same, false);
        var shorter = PathUtilities.IsPathInSubtree(custom, Path.Root.Append("field"), true);

        // assert
        Assert.True(rootIncludingSelf);
        Assert.False(rootStrict);
        Assert.False(equalLengthStrict);
        Assert.False(shorter);
    }

    private sealed class CustomPath : Path
    {
        public CustomPath()
        {
        }

        public CustomPath(Path parent) : base(parent)
        {
        }
    }
}
