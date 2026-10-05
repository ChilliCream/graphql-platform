namespace Mocha.Transport.Nats.Tests.Behaviors;

/// <summary>
/// Deliberately outside the <c>mocha.declared.contracts</c> namespace the test declares a stream
/// for, so the service still has a published subject that nothing it declared captures.
/// </summary>
public sealed record PalletLoaded(string PalletId);
