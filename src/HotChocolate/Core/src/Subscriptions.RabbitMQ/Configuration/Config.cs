using System;
using System.Text;
using RabbitMQ.Client;

namespace HotChocolate.Subscriptions.RabbitMQ.Configuration;

public delegate void Declare(IModel channel, string name);

public delegate void Bind(IModel channel, string exchangeName, string queueName);

public delegate void Publish(IModel channel, string exchangeName, byte[] body);

/// <summary>
/// Alters how RabbitMQPubSub performs ceratin actions.
/// To alter serialization, or naming conventions override implementation of ISerializer, IExchangeNameFactory or IQueueNameFactory.
/// Override declare methods to change exchange type, queue durability, exclusivity etc...
/// Override bind method to change how exchanges and queues connect, ie. setup routing scheme.
/// Override publish to change how are message published and routed.
/// </summary>
public class Config
{
    public Config()
    {
        DeclareExchange = (channel, name) => channel.ExchangeDeclare(name, ExchangeType.Direct);
        DeclareQueue = (channel, name) => channel.QueueDeclare(name, exclusive: true);
        BindQueue = (channel, exchangeName, queueName) => channel.QueueBind(queueName, exchangeName, "");
        PublishMessage = (channel, exchangeName, body) => channel.BasicPublish(exchangeName, "", null, body);
        InstanceName = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Override to change exchanges' type, durability, auto deletion etc...
    /// </summary>
    public Declare DeclareExchange { get; set; }
    /// <summary>
    /// Override to change queues' type, durability, auto deletion etc...
    /// </summary>
    public Declare DeclareQueue { get; set; }
    /// <summary>
    /// Override to change how exchanges and queues connect, ie. setup custom routing scheme.
    /// </summary>
    public Bind BindQueue { get; set; }
    /// <summary>
    /// Override to change how are messages published, required if setting up custom routing scheme.
    /// </summary>
    public Publish PublishMessage { get; set; }
    /// <summary>
    /// If true, when no ISourceStream is hooked to the consumer object, it will be disposed.
    /// This will lead to the deletion of a queue (if no other consumers exist) and exhange (if no queues exist) if declared as autodelete.
    /// </summary>
    public bool DisposeUnusedConsumers { get; set; }
    /// <summary>
    /// By default for every exchange each HC instance has own queue (and a consumer).
    /// Thus each queue has to hold a token identifying instance it belongs to.
    /// By default queue is named as a compination of exhcnage and instance name.
    /// Naming conventions can be override in IExchangeNameFactory and IQueueNameFactory.
    /// 
    /// If not set, a guid will be generated.
    /// </summary>
    public string InstanceName { get; set; }
}
