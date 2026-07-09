using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotChocolate.Fusion;

/// <summary>
/// Assertions for federation-gateway-audit test cases. Matches the audit runner
/// contract from <c>graphql-hive/federation-gateway-audit/src/test.ts</c>: deep-compare
/// the <c>data</c> payload and assert presence-of-errors independently.
/// </summary>
internal static class AuditAssertions
{
    /// <summary>
    /// Asserts that the gateway response matches the expected outcome.
    /// </summary>
    /// <param name="actualJson">
    /// The full gateway response serialized as JSON (i.e. <c>result.ToJson()</c>).
    /// </param>
    /// <param name="expectedDataJson">
    /// The expected <c>data</c> payload. When <see langword="null"/>, the <c>data</c>
    /// payload is not asserted.
    /// </param>
    /// <param name="expectsErrors">
    /// When not <see langword="null"/>, the presence of an <c>errors</c> array is
    /// asserted against this value.
    /// </param>
    /// <param name="expectedErrorPathJson">
    /// When not <see langword="null"/>, an error must have a deeply equal <c>path</c> array.
    /// </param>
    public static void Assert(
        string actualJson,
        string? expectedDataJson,
        bool? expectsErrors,
        string? expectedErrorPathJson = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(actualJson);

        var actual = JsonNode.Parse(actualJson)
            ?? throw new InvalidOperationException("Gateway response JSON parsed to null.");

        if (expectedDataJson is not null)
        {
            var actualData = actual["data"];
            var expectedData = JsonNode.Parse(expectedDataJson);

            if (!JsonNode.DeepEquals(actualData, expectedData))
            {
                var actualDataText = actualData?.ToJsonString(s_indented) ?? "null";
                var expectedDataText = expectedData?.ToJsonString(s_indented) ?? "null";
                var errorsText = actual["errors"]?.ToJsonString(s_indented) ?? "<none>";

                Xunit.Assert.Fail(
                    $"""
                     Data payload did not match.

                     Expected:
                     {expectedDataText}

                     Actual:
                     {actualDataText}

                     Errors:
                     {errorsText}
                     """);
            }
        }

        if (expectsErrors is not null)
        {
            var errors = actual["errors"];
            var hasErrors = errors is JsonArray { Count: > 0 };

            if (hasErrors != expectsErrors.Value)
            {
                var errorsText = errors?.ToJsonString(s_indented) ?? "<none>";

                Xunit.Assert.Fail(
                    expectsErrors.Value
                        ? $"Expected response to carry errors, but none were present. Response: {actualJson}"
                        : $"Expected response to carry no errors, but errors were present: {errorsText}");
            }
        }

        if (expectedErrorPathJson is not null)
        {
            var expectedErrorPath = JsonNode.Parse(expectedErrorPathJson);
            var errors = actual["errors"] as JsonArray;
            var hasExpectedPath = errors?.Any(
                error => JsonNode.DeepEquals(error?["path"], expectedErrorPath)) is true;

            if (!hasExpectedPath)
            {
                var errorsText = errors?.ToJsonString(s_indented) ?? "<none>";
                Xunit.Assert.Fail(
                    $"Expected an error with path {expectedErrorPathJson}, but found: {errorsText}");
            }
        }
    }

    private static readonly JsonSerializerOptions s_indented = new() { WriteIndented = true };
}
