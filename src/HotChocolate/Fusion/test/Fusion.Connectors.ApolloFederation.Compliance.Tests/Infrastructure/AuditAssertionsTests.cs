using Xunit.Sdk;

namespace HotChocolate.Fusion;

public sealed class AuditAssertionsTests
{
    [Fact]
    public void Assert_Should_RejectExecutionTimeout_When_ErrorsAreExpected()
    {
        var response = $$"""
            {
              "data": null,
              "errors": [
                {
                  "message": "The request exceeded the configured timeout.",
                  "extensions": {
                    "code": "{{ErrorCodes.Execution.Timeout}}"
                  }
                }
              ]
            }
            """;

        var exception = Assert.Throws<FailException>(
            () => AuditAssertions.Assert(response, expectedDataJson: null, expectsErrors: true));

        Assert.Equal(
            "A Fusion execution timeout cannot satisfy an official audit expectation.",
            exception.Message);
    }

    [Fact]
    public void Assert_Should_AcceptNonTimeoutError_When_ErrorsAreExpected()
    {
        const string response = """
            {
              "data": null,
              "errors": [
                {
                  "message": "The operation cannot be planned.",
                  "extensions": {
                    "code": "HC0010"
                  }
                }
              ]
            }
            """;

        AuditAssertions.Assert(response, expectedDataJson: null, expectsErrors: true);
    }
}
