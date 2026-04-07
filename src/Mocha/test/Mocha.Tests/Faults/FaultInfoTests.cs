using Mocha.Configuration.Faults;

namespace Mocha.Tests;

public class FaultInfoTests
{
    [Fact]
    public void FaultInfo_From_Should_CreateFromExceptionWithValidGuid_When_ExceptionProvided()
    {
        // arrange
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var exception = new InvalidOperationException("Fault test");

        // act
        var fault = FaultInfo.From(id, timestamp, exception);

        // assert
        Assert.NotNull(fault);
        Assert.Equal(id, fault.Id);
        Assert.Equal(timestamp, fault.Timestamp);
        Assert.Equal("Exception", fault.ErrorCode);
        Assert.NotNull(fault.Exceptions);
        Assert.Single(fault.Exceptions);
    }

    [Fact]
    public void FaultInfo_From_Should_PopulateExceptionDetails_When_ExceptionProvided()
    {
        // arrange
        var exception = new InvalidOperationException("Test error");

        // act
        var fault = FaultInfo.From(Guid.NewGuid(), DateTimeOffset.UtcNow, exception);

        // assert
        Assert.NotNull(fault.Exceptions[0]);
        Assert.Equal("System.InvalidOperationException", fault.Exceptions[0].ExceptionType);
        Assert.Equal("Test error", fault.Exceptions[0].Message);
    }

    [Fact]
    public void FaultInfo_From_Should_CreateFromNestedException_When_InnerExceptionExists()
    {
        // arrange
        var innerException = new ArgumentException("Inner error");
        var outerException = new InvalidOperationException("Outer error", innerException);

        // act
        var fault = FaultInfo.From(Guid.NewGuid(), DateTimeOffset.UtcNow, outerException);

        // assert
        Assert.NotNull(fault);
        Assert.Single(fault.Exceptions);
        Assert.Equal("System.InvalidOperationException", fault.Exceptions[0].ExceptionType);
    }
}
