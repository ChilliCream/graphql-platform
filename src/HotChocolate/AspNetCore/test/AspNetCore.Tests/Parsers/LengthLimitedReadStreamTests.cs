namespace HotChocolate.AspNetCore.Parsers;

public class LengthLimitedReadStreamTests
{
    [Fact]
    public async Task ReadAsync_Should_ReturnEveryByte_When_LengthEqualsLimit()
    {
        // arrange
        var stream = new LengthLimitedReadStream(
            new MemoryStream("abcd"u8.ToArray()),
            4,
            static () => new InvalidOperationException("over the limit"));
        using var reader = new StreamReader(stream);

        // act
        var content = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("abcd", content);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowGivenException_When_LengthExceedsLimit()
    {
        // arrange
        var stream = new LengthLimitedReadStream(
            new MemoryStream("abcde"u8.ToArray()),
            4,
            static () => new InvalidOperationException("over the limit"));
        using var reader = new StreamReader(stream);

        // act
        async Task Action() => await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Action);
        Assert.Equal("over the limit", exception.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowGivenException_When_ArrayOverloadExceedsLimit()
    {
        // arrange
        var stream = new LengthLimitedReadStream(
            new MemoryStream("abcde"u8.ToArray()),
            4,
            static () => new InvalidOperationException("over the limit"));
        var buffer = new byte[16];

        // act
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
        async Task<int> Action() => await stream.ReadAsync(
            buffer,
            0,
            buffer.Length,
            TestContext.Current.CancellationToken);
#pragma warning restore CA1835

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Action);
        Assert.Equal("over the limit", exception.Message);
    }

    [Fact]
    public void Read_Should_ThrowGivenException_When_ArrayOverloadExceedsLimit()
    {
        // arrange
        var stream = new LengthLimitedReadStream(
            new MemoryStream("abcde"u8.ToArray()),
            4,
            static () => new InvalidOperationException("over the limit"));
        var buffer = new byte[4];
        var bytesReadAtLimit = stream.Read(buffer, 0, buffer.Length);

        // act
        object Action() => stream.Read(buffer, 0, buffer.Length);

        // assert
        Assert.Equal(4, bytesReadAtLimit);
        var exception = Assert.Throws<InvalidOperationException>(Action);
        Assert.Equal("over the limit", exception.Message);
    }

    [Fact]
    public void Read_Should_ThrowGivenException_When_LengthExceedsLimit()
    {
        // arrange
        var stream = new LengthLimitedReadStream(
            new MemoryStream("abcde"u8.ToArray()),
            4,
            static () => new InvalidOperationException("over the limit"));
        var buffer = new byte[16];

        // act
        void Action() => stream.ReadExactly(buffer, 0, 5);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Action);
        Assert.Equal("over the limit", exception.Message);
    }
}
