namespace HotChocolate;

public class ErrorTests
{
    [Fact]
    public void WithCode()
    {
        // arrange
        IError error = new Error("123");

        // act
        error = error.WithCode("foo");

        // assert
        Assert.Equal("foo", error.Code);
    }

    [Fact]
    public void RemoveCode()
    {
        // arrange
        IError error = new Error("123", code: "foo");

        // act
        error = error.RemoveCode();

        // assert
        Assert.Null(error.Code);
    }

    [Fact]
    public void WithException()
    {
        // arrange
        IError error = new Error
        (
            "123"
        );

        var exception = new Exception();

        // act
        error = error.WithException(exception);

        // assert
        Assert.Equal(exception, error.Exception);
    }

    [Fact]
    public void RemoveException()
    {
        // arrange
        IError error = new Error
        (
            "123",
            exception: new Exception()
        );

        Assert.NotNull(error.Exception);

        // act
        error = error.RemoveException();

        // assert
        Assert.Null(error.Exception);
    }

    [Fact]
    public void WithExtensions()
    {
        // arrange
        IError error = new Error("123");

        // act
        error = error.WithExtensions(
            new Dictionary<string, object?> { { "a", "b" }, });

        // assert
        Assert.Collection(
            error.Extensions!,
            t =>
            {
                Assert.Equal("a", t.Key);
                Assert.Equal("b", t.Value);
            });
    }

    [Fact]
    public void AddExtensions()
    {
        // arrange
        IError error = new Error("123");

        // act
        error = error.SetExtension("a", "b").SetExtension("c", "d");

        // assert
        Assert.Collection(
            error.Extensions!.OrderBy(t => t.Key),
            t =>
            {
                Assert.Equal("a", t.Key);
                Assert.Equal("b", t.Value);
            },
            t =>
            {
                Assert.Equal("c", t.Key);
                Assert.Equal("d", t.Value);
            });
    }

    [Fact]
    public void RemoveExtensions()
    {
        // arrange
        IError error = new Error("123");
        error = error.WithExtensions(
            new Dictionary<string, object?>
            {
                { "a", "b" },
                { "c", "d" },
            });

        // act
        error = error.RemoveExtension("a");

        // assert
        Assert.Collection(
            error.Extensions!,
            t =>
            {
                Assert.Equal("c", t.Key);
                Assert.Equal("d", t.Value);
            });
    }

    [Fact]
    public void WithLocations()
    {
        // arrange
        IError error = new Error("123");

        // act
        error = error.WithLocations(new List<Location> { new(1, 2), });

        // assert
        Assert.Collection(
            error.Locations!,
            t =>
            {
                Assert.Equal(1, t.Line);
                Assert.Equal(2, t.Column);
            });
    }

    [Fact]
    public void WithMessage()
    {
        // arrange
        IError error = new Error("123");

        // act
        error = error.WithMessage("456");

        // assert
        Assert.Equal("456", error.Message);
    }

    [Fact]
    public void WithMessage_MessageNull_ArgumentException()
    {
        // arrange
        IError error = new Error("123");

        // act
        Action action = () => error.WithMessage(null!);

        // assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void WithMessage_MessageEmpty_ArgumentException()
    {
        // arrange
        IError error = new Error("123");

        // act
        Action action = () => error.WithMessage(string.Empty);

        // assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void WithPath()
    {
        // arrange
        IError error = new Error("123");

        // act
        error = error.WithPath(Path.FromList("foo"));

        // assert
        Assert.Equal("/foo", error.Path!.Print());
    }
}
