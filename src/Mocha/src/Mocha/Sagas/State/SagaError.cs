namespace Mocha.Sagas;

/// <summary>
/// Represents an error that occurred during saga execution, recording the state in which it happened and a description.
/// </summary>
/// <param name="CurrentState">The name of the saga state when the error occurred.</param>
/// <param name="Message">A description of the error.</param>
public sealed record SagaError(string CurrentState, string Message);
