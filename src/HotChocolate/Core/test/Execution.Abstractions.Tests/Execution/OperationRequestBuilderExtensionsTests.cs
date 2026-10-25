using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Types;

namespace HotChocolate.Execution;

public class OperationRequestBuilderExtensionsTests
{
    [Fact]
    public void AddFile_Should_RegisterFileInLookup_When_NamedFileProvided()
    {
        // arrange
        var builder = OperationRequestBuilder.New().SetDocument("{ foo }");
        var file = new TestFile("a.txt");

        // act
        builder.AddFile("variables.file", file);

        // assert
        var lookup = builder.Features.Get<IFileLookup>();
        Assert.NotNull(lookup);
        Assert.True(lookup.TryGetFile("variables.file", out var resolved));
        Assert.Same(file, resolved);
    }

    [Fact]
    public void AddFile_Should_RegisterAllFiles_When_CalledMultipleTimes()
    {
        // arrange
        var builder = OperationRequestBuilder.New().SetDocument("{ foo }");
        var first = new TestFile("a.txt");
        var second = new TestFile("b.txt");

        // act
        builder.AddFile("0", first).AddFile("1", second);

        // assert
        var lookup = builder.Features.GetRequired<IFileLookup>();
        Assert.True(lookup.TryGetFile("0", out var resolvedFirst));
        Assert.True(lookup.TryGetFile("1", out var resolvedSecond));
        Assert.Same(first, resolvedFirst);
        Assert.Same(second, resolvedSecond);
    }

    [Fact]
    public void AddFile_Should_ReuseSingleLookupFeature_When_CalledMultipleTimes()
    {
        // arrange
        var builder = OperationRequestBuilder.New().SetDocument("{ foo }");

        // act
        builder
            .AddFile("0", new TestFile("a.txt"))
            .AddFile("1", new TestFile("b.txt"));

        // assert
        var lookupAfterFirst = builder.Features.Get<IFileLookup>();
        builder.AddFile("2", new TestFile("c.txt"));
        var lookupAfterThird = builder.Features.Get<IFileLookup>();
        Assert.Same(lookupAfterFirst, lookupAfterThird);
    }

    [Fact]
    public void AddFile_Should_OverwriteEntry_When_SameNameUsedTwice()
    {
        // arrange
        var builder = OperationRequestBuilder.New().SetDocument("{ foo }");
        var initial = new TestFile("a.txt");
        var replacement = new TestFile("b.txt");

        // act
        builder.AddFile("file", initial).AddFile("file", replacement);

        // assert
        var lookup = builder.Features.GetRequired<IFileLookup>();
        Assert.True(lookup.TryGetFile("file", out var resolved));
        Assert.Same(replacement, resolved);
    }

    [Fact]
    public void AddFile_Should_Throw_When_BuilderIsNull()
    {
        // arrange
        OperationRequestBuilder builder = null!;

        // act
        void Act() => builder.AddFile("file", new TestFile("a.txt"));

        // assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void AddFile_Should_Throw_When_FileIsNull()
    {
        // arrange
        var builder = OperationRequestBuilder.New().SetDocument("{ foo }");

        // act
        void Act() => builder.AddFile("file", null!);

        // assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void AddFile_Should_Throw_When_NameIsNull()
    {
        // arrange
        var builder = OperationRequestBuilder.New().SetDocument("{ foo }");

        // act
        void Act() => builder.AddFile(null!, new TestFile("a.txt"));

        // assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void AddFile_Should_Throw_When_NameIsEmpty()
    {
        // arrange
        var builder = OperationRequestBuilder.New().SetDocument("{ foo }");

        // act
        void Act() => builder.AddFile(string.Empty, new TestFile("a.txt"));

        // assert
        Assert.Throws<ArgumentException>(Act);
    }

    [Fact]
    public void AddFile_Should_Throw_When_DifferentFileLookupAlreadySet()
    {
        // arrange
        var builder = OperationRequestBuilder.New().SetDocument("{ foo }");
        builder.Features.Set<IFileLookup>(new ForeignFileLookup());

        // act
        void Act() => builder.AddFile("file", new TestFile("a.txt"));

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    private sealed class ForeignFileLookup : IFileLookup
    {
        public bool TryGetFile(string name, [NotNullWhen(true)] out IFile? file)
        {
            file = null;
            return false;
        }
    }

    private sealed class TestFile(string name) : IFile
    {
        public string Name { get; } = name;

        public long? Length => null;

        public string? ContentType => null;

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Stream OpenReadStream() => Stream.Null;
    }
}
