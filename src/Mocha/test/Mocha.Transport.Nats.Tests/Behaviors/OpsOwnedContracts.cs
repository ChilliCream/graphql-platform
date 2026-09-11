namespace Mocha.Ops.Contracts;

/// <summary>
/// A separate namespace from the other shared-stream contracts, so the ops-owned stream can capture
/// its own subject space without overlapping another test's stream.
/// </summary>
public sealed record InvoiceRaised(string InvoiceId);
