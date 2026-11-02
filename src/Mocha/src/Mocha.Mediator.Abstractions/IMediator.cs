namespace Mocha.Mediator;

/// <summary>
/// Defines a combined mediator that can send commands, queries, and publish notifications.
/// </summary>
/// <remarks>
/// This interface inherits from both <see cref="ISender"/> and <see cref="IPublisher"/>,
/// providing a single entry point for all mediator operations.
/// </remarks>
public interface IMediator : ISender, IPublisher;
