namespace Mocha;

/// <summary>
/// Describes a consumer for diagnostic and visualization purposes.
/// </summary>
/// <param name="Name">The logical name of the consumer.</param>
/// <param name="IdentityType">The short type name of the handler this consumer wraps.</param>
/// <param name="IdentityTypeFullName">The fully qualified type name of the handler, or <c>null</c> if unavailable.</param>
/// <param name="SagaName">The name of the associated saga, or <c>null</c> if this consumer is not part of a saga.</param>
/// <param name="IsBatch">Whether this consumer processes messages in batches.</param>
public sealed record ConsumerDescription(
    string Name,
    string IdentityType,
    string? IdentityTypeFullName,
    string? SagaName,
    bool IsBatch);
