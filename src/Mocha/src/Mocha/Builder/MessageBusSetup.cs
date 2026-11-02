namespace Mocha;

internal sealed class MessageBusSetup
{
    public List<Action<MessageBusBuilder>> ConfigureMessageBus { get; set; } = [];
}
