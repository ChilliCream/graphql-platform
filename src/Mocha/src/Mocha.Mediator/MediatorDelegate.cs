namespace Mocha.Mediator;

/// <summary>
/// Represents an asynchronous operation in the mediator pipeline.
/// </summary>
public delegate ValueTask MediatorDelegate(IMediatorContext context);
