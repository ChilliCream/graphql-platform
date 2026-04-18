using System.Globalization;
using System.Text.Json;

namespace Mocha.Tests;

public class HeadersJsonConverterTests
{
    [Fact]
    public void Write_Should_EmitStringValue_When_HeaderIsGuid()
    {
        // arrange
        var headers = new Headers();
        var value = Guid.Parse("12345678-1234-1234-1234-123456789012");
        headers.Set("id", value);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"id":"12345678-1234-1234-1234-123456789012"}""", json);
    }

    [Fact]
    public void Write_Should_EmitISOString_When_HeaderIsTimeSpan()
    {
        // arrange
        var headers = new Headers();
        var value = new TimeSpan(1, 2, 3, 4);
        headers.Set("duration", value);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        var expected = value.ToString("c", CultureInfo.InvariantCulture);
        Assert.Equal($$"""{"duration":"{{expected}}"}""", json);
    }

    [Fact]
    public void Write_Should_EmitString_When_HeaderIsUri()
    {
        // arrange
        var headers = new Headers();
        var value = new Uri("https://example.com/path");
        headers.Set("url", value);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"url":"https://example.com/path"}""", json);
    }

    [Fact]
    public void Write_Should_EmitISODate_When_HeaderIsDateOnly()
    {
        // arrange
        var headers = new Headers();
        var value = new DateOnly(2024, 1, 15);
        headers.Set("date", value);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        var expected = value.ToString("O", CultureInfo.InvariantCulture);
        Assert.Equal($$"""{"date":"{{expected}}"}""", json);
    }

    [Fact]
    public void Write_Should_EmitISOTime_When_HeaderIsTimeOnly()
    {
        // arrange
        var headers = new Headers();
        var value = new TimeOnly(10, 30, 45);
        headers.Set("time", value);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        var expected = value.ToString("O", CultureInfo.InvariantCulture);
        Assert.Equal($$"""{"time":"{{expected}}"}""", json);
    }

    [Fact]
    public void Write_Should_EmitEnumName_When_HeaderIsEnum()
    {
        // arrange
        var headers = new Headers();
        headers.Set("priority", TestPriority.High);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"priority":"High"}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsShort()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", (short)123);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":123}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsUshort()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", (ushort)456);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":456}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsByte()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", (byte)7);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":7}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsSbyte()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", (sbyte)-8);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":-8}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsUint()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", 4000000000u);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":4000000000}""", json);
    }

    [Fact]
    public void Write_Should_EmitNumber_When_HeaderIsUlong()
    {
        // arrange
        var headers = new Headers();
        headers.Set("value", 18000000000000000000ul);

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"value":18000000000000000000}""", json);
    }

    [Fact]
    public void Write_Should_EmitStringValue_When_HeaderIsChar()
    {
        // arrange
        var headers = new Headers();
        headers.Set("letter", 'A');

        // act
        var json = JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options);

        // assert
        Assert.Equal("""{"letter":"A"}""", json);
    }

    [Fact]
    public void Write_Should_Throw_When_HeaderIsCustomType()
    {
        // arrange
        var headers = new Headers();
        headers.Set("custom", new CustomHeaderDto { Name = "test" });

        // act & assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => JsonSerializer.Serialize<IHeaders>(headers, HeadersJsonConverter.Options));
        Assert.Contains(nameof(CustomHeaderDto), ex.Message);
    }

    private enum TestPriority
    {
        Low,
        Normal,
        High
    }

    private sealed class CustomHeaderDto
    {
        public string Name { get; init; } = "";
    }
}
