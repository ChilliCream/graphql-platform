namespace HotChocolate.Fusion.Execution;

internal readonly record struct PolicyDecision(bool IsDenied, string? Reason);
