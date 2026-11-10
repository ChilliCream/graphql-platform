namespace HotChocolate.Fusion.Policies;

internal sealed class PolicyNotResolvedException(string message) : InvalidOperationException(message);
