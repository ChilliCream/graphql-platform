namespace HotChocolate.Fusion;

/// <summary>
/// Describes a single federation-gateway-audit test case:
/// a GraphQL query and its expected response (data and/or errors).
/// </summary>
/// <param name="Query">The GraphQL operation to execute against the gateway.</param>
/// <param name="ExpectedData">
/// The expected JSON payload of the <c>data</c> field, or <see langword="null"/> if the
/// assertion should not compare the <c>data</c> payload.
/// </param>
/// <param name="ExpectsErrors">
/// If not <see langword="null"/>, the test asserts whether an <c>errors</c> array is
/// present on the response.
/// </param>
public sealed record AuditTestCase(
    string Query,
    string? ExpectedData,
    bool? ExpectsErrors);
