namespace Mocha.Analyzers;

/// <summary>
/// Represents the kind of call site detected for a message dispatch invocation.
/// </summary>
public enum CallSiteKind
{
    /// <summary>
    /// A <c>PublishAsync&lt;T&gt;</c> call on <c>IMessageBus</c>.
    /// </summary>
    Publish,

    /// <summary>
    /// A <c>SendAsync&lt;T&gt;</c> call on <c>IMessageBus</c>.
    /// </summary>
    Send,

    /// <summary>
    /// A <c>SchedulePublishAsync&lt;T&gt;</c> call on <c>IMessageBus</c>.
    /// </summary>
    SchedulePublish,

    /// <summary>
    /// A <c>ScheduleSendAsync&lt;T&gt;</c> call on <c>IMessageBus</c>.
    /// </summary>
    ScheduleSend,

    /// <summary>
    /// A <c>RequestAsync&lt;T&gt;</c> call on <c>IMessageBus</c>.
    /// </summary>
    Request,

    /// <summary>
    /// A <c>SendAsync</c> call on <c>Mocha.Mediator.ISender</c>.
    /// </summary>
    MediatorSend,

    /// <summary>
    /// A <c>QueryAsync</c> call on <c>Mocha.Mediator.ISender</c>.
    /// </summary>
    MediatorQuery,

    /// <summary>
    /// A <c>PublishAsync&lt;T&gt;</c> call on <c>Mocha.Mediator.IPublisher</c>.
    /// </summary>
    MediatorPublish
}
