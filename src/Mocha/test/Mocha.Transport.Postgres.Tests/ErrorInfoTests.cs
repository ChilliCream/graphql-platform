using System.Text.Json;

namespace Mocha.Transport.Postgres.Tests;

public class ErrorInfoTests
{
    [Fact]
    public void From_Should_CaptureExceptionType_When_ExceptionProvided()
    {
        // arrange
        var exception = new InvalidOperationException("Test error");

        // act
        var errorInfo = ErrorInfo.From(exception);

        // assert
        Assert.Equal("InvalidOperationException", errorInfo.ExceptionType);
    }

    [Fact]
    public void From_Should_CaptureMessage_When_ExceptionProvided()
    {
        // arrange
        var exception = new ArgumentException("Bad argument");

        // act
        var errorInfo = ErrorInfo.From(exception);

        // assert
        Assert.Equal("Bad argument", errorInfo.Message);
    }

    [Fact]
    public void From_Should_CaptureStackTrace_When_ExceptionHasStackTrace()
    {
        // arrange
        Exception exception;
        try
        {
            throw new InvalidOperationException("Stack trace test");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // act
        var errorInfo = ErrorInfo.From(exception);

        // assert
        Assert.NotNull(errorInfo.StackTrace);
        Assert.Contains("ErrorInfoTests", errorInfo.StackTrace);
    }

    [Fact]
    public void From_Should_HandleNullStackTrace_When_ExceptionNotThrown()
    {
        // arrange
        var exception = new InvalidOperationException("No stack trace");

        // act
        var errorInfo = ErrorInfo.From(exception);

        // assert
        Assert.Null(errorInfo.StackTrace);
    }

    [Fact]
    public void ErrorInfo_Should_SerializeToJson_When_JsonPropertyNamesUsed()
    {
        // arrange
        var errorInfo = new ErrorInfo("TestException", "Test message", "at Test.Method()");

        // act
        var json = JsonSerializer.Serialize(errorInfo);

        // assert
        Assert.Contains("\"type\":\"TestException\"", json);
        Assert.Contains("\"message\":\"Test message\"", json);
        Assert.Contains("\"stackTrace\":\"at Test.Method()\"", json);
    }

    [Fact]
    public void ErrorInfo_Should_DeserializeFromJson_When_ValidJson()
    {
        // arrange
        const string json = """{"type":"ArgumentException","message":"Bad arg","stackTrace":null}""";

        // act
        var errorInfo = JsonSerializer.Deserialize<ErrorInfo>(json);

        // assert
        Assert.NotNull(errorInfo);
        Assert.Equal("ArgumentException", errorInfo!.ExceptionType);
        Assert.Equal("Bad arg", errorInfo.Message);
        Assert.Null(errorInfo.StackTrace);
    }
}
