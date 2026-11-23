using System.Text;
using ChilliCream.Nitro.CommandLine.Helpers;

namespace ChilliCream.Nitro.CommandLine.Tests.Helpers;

public sealed class OperationsFileReaderTests
{
    private static readonly string s_largeBody = "query Large { " + new string('a', 10_000) + " }";

    [Fact]
    public async Task OpenAsync_Should_ReturnFileStream_When_FileIsRelayStyle()
    {
        // arrange
        var fileStream = CreateStream($$"""{ "a1": "{{s_largeBody}}", "format": "relay" }""");

        // act
        await using var result = await OperationsFileReader.OpenAsync(fileStream, CancellationToken.None);

        // assert
        Assert.Same(fileStream, result);
        Assert.Equal(0, result.Position);
    }

    [Fact]
    public async Task OpenAsync_Should_ConvertManifest_When_FormatFollowsLargeOperations()
    {
        // arrange
        var fileStream = CreateStream(
            $$"""
            {
              "operations": [{ "id": "a1", "body": "{{s_largeBody}}", "name": "Large", "type": "query" }],
              "version": 1,
              "format": "apollo-persisted-query-manifest"
            }
            """);

        // act
        await using var result = await OperationsFileReader.OpenAsync(fileStream, CancellationToken.None);

        // assert
        Assert.Equal($$"""{"a1":"{{s_largeBody}}"}""", await ReadAsync(result));
    }

    [Fact]
    public async Task OpenAsync_Should_ConvertManifest_When_FileStartsWithBom()
    {
        // arrange
        var fileStream = CreateStream(
            """
            {
              "format": "apollo-persisted-query-manifest",
              "version": 1,
              "operations": [{ "id": "a1", "body": "query A { a }", "name": "A", "type": "query" }]
            }
            """,
            withBom: true);

        // act
        await using var result = await OperationsFileReader.OpenAsync(fileStream, CancellationToken.None);

        // assert
        Assert.Equal("""{"a1":"query A { a }"}""", await ReadAsync(result));
    }

    [Fact]
    public async Task OpenAsync_Should_ConvertManifest_When_StreamIsNotSeekable()
    {
        // arrange
        var fileStream = new NonSeekableStream(CreateStream(
            """
            {
              "format": "apollo-persisted-query-manifest",
              "version": 1,
              "operations": [{ "id": "a1", "body": "query A { a }", "name": "A", "type": "query" }]
            }
            """));

        // act
        await using var result = await OperationsFileReader.OpenAsync(fileStream, CancellationToken.None);

        // assert
        Assert.Equal("""{"a1":"query A { a }"}""", await ReadAsync(result));
    }

    [Fact]
    public async Task OpenAsync_Should_ReturnContentUnchanged_When_JsonIsMalformed()
    {
        // arrange
        const string content = """{ "a1": "query A { a }", """;
        var fileStream = CreateStream(content);

        // act
        await using var result = await OperationsFileReader.OpenAsync(fileStream, CancellationToken.None);

        // assert
        Assert.Equal(content, await ReadAsync(result));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("1")]
    [InlineData("""{ "body": "query B { b }" }""")]
    public async Task OpenAsync_Should_Throw_When_RelayOperationIsNotAString(string operation)
    {
        // arrange
        var fileStream = CreateStream(
            $$"""
            {
              "a1": "{{s_largeBody}}",
              "b2": {{operation}},
              "c3": "query C { c }"
            }
            """);

        // act
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => OperationsFileReader.OpenAsync(fileStream, CancellationToken.None));

        // assert
        Assert.Equal(
            "The operations file is not valid. The operation with ID 'b2' must be a string.",
            exception.Message);
    }

    [Theory]
    [InlineData("""{ "body": "query A { a }" }""")]
    [InlineData("""{ "id": "a1" }""")]
    [InlineData("""{ "id": 1, "body": "query A { a }" }""")]
    public async Task OpenAsync_Should_Throw_When_ManifestOperationHasNoStringIdOrBody(string operation)
    {
        // arrange
        var fileStream = CreateStream(
            $$"""
            {
              "format": "apollo-persisted-query-manifest",
              "version": 1,
              "operations": [{{operation}}]
            }
            """);

        // act
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => OperationsFileReader.OpenAsync(fileStream, CancellationToken.None));

        // assert
        Assert.Equal(
            "The operations file is not a valid Apollo persisted query manifest. "
            + "The operation at index 0 must have a string 'id' and a string 'body'.",
            exception.Message);
    }

    private static MemoryStream CreateStream(string content, bool withBom = false)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new MemoryStream(withBom ? [0xEF, 0xBB, 0xBF, .. bytes] : bytes);
    }

    private static async Task<string> ReadAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
