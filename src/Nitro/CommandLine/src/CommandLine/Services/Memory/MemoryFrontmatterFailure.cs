namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// A parse failure: the reason and a message describing it in detail.
/// </summary>
internal sealed record MemoryFrontmatterFailure(MemoryFrontmatterFailureReason Reason, string Message);
