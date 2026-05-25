namespace Mocha;

internal sealed class MessageBusSetup
{
    public List<Action<IMessageBusBuilder>> ConfigureMessageBus { get; set; } = [];
}
