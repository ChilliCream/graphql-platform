using System;
using System.Text;
using RabbitMQ.Client;

namespace HotChocolate.Subscriptions.RabbitMQ.Configuration;

public delegate void Declare(IModel channel, string name);

public delegate void Bind(IModel channel, string exchangeName, string queueName);

public delegate void Publish(IModel channel, string exchangeName, byte[] body);

/// <summary>
/// Override declare methods to change exchange type, queue's durability, exclusivity etc...
/// Override bind method to change how exchanges and queues connect, ie. setup routing scheme.
/// Override publish to change how are message published and routed.
/// </summary>
public class Config
{
    /// <summary>
    /// Override to to change exchanges' type, durability, auto deletion etc...
    /// </summary>
    public Declare DeclareExchange { get; set; }
    /// <summary>
    /// Override to to change queues' type, durability, auto deletion etc...
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
    /// This will lead to the deletion of a queue (if no other consumers exist) and exhange (if no queues exist) if overriden as autodelete.
    /// </summary>
    public bool DisposeUnusedConsumers { get; set; }
    /// <summary>
    /// As there is single exhcnage per HC's topic and each HC instance holds a unique queue to an exchange, multiple HC instances should not share a single queue.
    /// If multiple HC instances subscribe to a single queue, RabbitMQ will automatically perform round-robin between queue's consumers.
    /// By default queue is a combination of exhcnage name and instance name.
    /// If instance name is not set, guid will be generated.
    /// </summary>
    public string InstanceName { get; set; }

    public Config()
    {
        DeclareExchange = (channel, name) => channel.ExchangeDeclare(name, ExchangeType.Direct);
        DeclareQueue = (channel, name) => channel.QueueDeclare(name);
        BindQueue = (channel, exchangeName, queueName) => channel.QueueBind(queueName, exchangeName, "");
        PublishMessage = (channel, exchangeName, body) => channel.BasicPublish(exchangeName, "", null, body);
        InstanceName = Guid.NewGuid().ToString();
    }
}
