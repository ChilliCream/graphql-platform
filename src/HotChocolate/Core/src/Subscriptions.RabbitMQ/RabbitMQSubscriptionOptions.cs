namespace HotChocolate.Subscriptions.RabbitMQ;

public class RabbitMQSubscriptionOptions
{
    /// <summary>
    /// The prefix for the queue name.
    /// </summary>
    public string QueuePrefix { get; set; } = string.Empty;
}
