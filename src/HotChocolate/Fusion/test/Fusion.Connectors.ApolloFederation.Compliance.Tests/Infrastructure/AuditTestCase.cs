namespace HotChocolate.Fusion;

/// <summary>
/// Describes a single federation-gateway-audit test case:
/// a GraphQL query and its expected response (data and/or errors).
/// </summary>
/// <param name="Id">The stable official suite and ordered case identifier.</param>
/// <param name="Query">The GraphQL operation to execute against the gateway.</param>
/// <param name="Variables">The optional JSON object containing operation variables.</param>
/// <param name="HasExpectedData">
/// Indicates whether the upstream test explicitly declares a <c>data</c> member.
/// </param>
/// <param name="ExpectedData">
/// The expected JSON payload of the <c>data</c> field, or <see langword="null"/> if the
/// assertion should not compare the <c>data</c> payload.
/// </param>
/// <param name="HasExpectedErrors">
/// Indicates whether the upstream test explicitly declares an <c>errors</c> member.
/// </param>
/// <param name="ExpectsErrors">
/// If not <see langword="null"/>, the test asserts whether an <c>errors</c> array is
/// present on the response.
/// </param>
public sealed record AuditTestCase(
    string Id,
    string Query,
    string? Variables,
    bool HasExpectedData,
    string? ExpectedData,
    bool HasExpectedErrors,
    bool? ExpectsErrors);
