using HotChocolate.Collections.Immutable;

namespace HotChocolate.Fusion.Execution;

public sealed class ErrorHelperTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WithPath_Should_CopyError_When_MetadataIsImmutable(bool hasExtensions)
    {
        // arrange
        var source = new Error
        {
            Message = "boom",
            Path = Path.Root.Append("source"),
            Exception = new InvalidOperationException("source exception"),
            Extensions = hasExtensions
                ? ImmutableOrderedDictionary<string, object?>.Empty.Add("code", "TEST")
                : null
        };
        var path = Path.Root.Append("target");

        // act
        var mapped = ErrorHelper.WithPath(source, path);

        // assert
        Assert.Equal(source with { Path = path }, mapped);
        Assert.NotSame(source, mapped);
    }

    [Fact]
    public void WithPath_Should_KeepNestedValueReferences_When_MetadataIsImmutable()
    {
        // arrange
        var details = new List<string> { "before" };
        var source = new Error
        {
            Message = "boom",
            Extensions = ImmutableOrderedDictionary<string, object?>.Empty.Add("details", details)
        };

        // act
        var mapped = ErrorHelper.WithPath(source, Path.Root.Append("target"));
        details.Add("after");

        // assert
        Assert.Same(source.Extensions, mapped.Extensions);
        Assert.Equal(new[] { "before", "after" }, Assert.IsType<List<string>>(mapped.Extensions!["details"]));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WithPath_Should_SnapshotMutableMetadata_When_SourceCollectionsChange(bool hasLocations)
    {
        // arrange
        var extensions = new Dictionary<string, object?> { ["custom"] = "before" };
        var locations = new List<Location> { new(1, 2) };
        var source = new Error
        {
            Message = "boom",
            Extensions = extensions,
            Locations = hasLocations ? locations : null
        };
        var path = Path.Root.Append("target");

        // act
        var mapped = ErrorHelper.WithPath(source, path);
        extensions["custom"] = "after";
        locations.Add(new Location(3, 4));

        // assert
        Assert.Equal("before", mapped.Extensions!["custom"]);
        Assert.Equal(hasLocations ? new[] { new Location(1, 2) } : null, mapped.Locations);
        Assert.Equal(path, mapped.Path);
    }

    [Fact]
    public void WithPath_Should_SnapshotLocations_When_ExtensionsAreImmutable()
    {
        // arrange
        var locations = new List<Location> { new(1, 2) };
        var source = new Error
        {
            Message = "boom",
            Extensions = ImmutableOrderedDictionary<string, object?>.Empty.Add("code", "TEST"),
            Locations = locations
        };

        // act
        var mapped = ErrorHelper.WithPath(source, Path.Root.Append("target"));
        locations.Add(new Location(3, 4));

        // assert
        Assert.Equal(new[] { new Location(1, 2) }, mapped.Locations);
        Assert.NotSame(source.Extensions, mapped.Extensions);
    }

    [Fact]
    public void WithPath_Should_CreateBaseError_When_SourceIsDerivedError()
    {
        // arrange
        var source = new AggregateError(new Error { Message = "inner" });
        var path = Path.Root.Append("target");

        // act
        var mapped = ErrorHelper.WithPath(source, path);

        // assert
        Assert.IsType<Error>(mapped);
        Assert.Equal(source.Message, mapped.Message);
        Assert.Equal(path, mapped.Path);
    }

    [Fact]
    public void WithPath_Should_SnapshotMutableMetadata_When_SourceImplementsIError()
    {
        // arrange
        var extensions = new Dictionary<string, object?> { ["custom"] = "before" };
        var source = new DelegatingError(new Error { Message = "boom", Extensions = extensions });
        var path = Path.Root.Append("target");

        // act
        var mapped = ErrorHelper.WithPath(source, path);
        extensions["custom"] = "after";

        // assert
        Assert.IsType<Error>(mapped);
        Assert.Equal("before", mapped.Extensions!["custom"]);
        Assert.Equal(path, mapped.Path);
    }

    private sealed class DelegatingError(Error error) : IError
    {
        public string Message => error.Message;
        public string? Code => error.Code;
        public Path? Path => error.Path;
        public IReadOnlyList<Location>? Locations => error.Locations;
        public IReadOnlyDictionary<string, object?>? Extensions => error.Extensions;
        public Exception? Exception => error.Exception;
        public IError WithMessage(string message) => error.WithMessage(message);
        public IError WithCode(string? code) => error.WithCode(code);
        public IError WithPath(Path path) => error.WithPath(path);
        public IError WithLocations(IReadOnlyList<Location> locations) => error.WithLocations(locations);
        public IError WithExtensions(IReadOnlyDictionary<string, object?>? extensions) => error.WithExtensions(extensions);
        public IError SetExtension(string key, object? value) => error.SetExtension(key, value);
        public IError RemoveExtension(string key) => error.RemoveExtension(key);
        public IError WithException(Exception? exception) => error.WithException(exception);
    }
}
