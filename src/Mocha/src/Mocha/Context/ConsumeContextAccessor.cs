namespace Mocha;

/// <summary>
/// Scoped service that holds a reference to the current <see cref="IConsumeContext"/>
/// during message consumption. Used by <see cref="DefaultMessageBus"/> to automatically
/// propagate ConversationId and CausationId when publishing or sending from within a handler.
/// </summary>
public sealed class ConsumeContextAccessor
{
    public IConsumeContext? Context { get; set; }
}
