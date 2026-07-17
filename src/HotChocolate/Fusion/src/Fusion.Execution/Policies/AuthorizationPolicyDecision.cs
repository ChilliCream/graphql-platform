namespace HotChocolate.Fusion.Execution;

internal readonly record struct AuthorizationPolicyDecision(bool IsDenied, string? Reason);
