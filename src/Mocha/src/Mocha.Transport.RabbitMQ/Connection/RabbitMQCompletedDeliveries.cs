namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Remembers the ids of messages whose handler completed but whose delivery could not be acknowledged, because its
/// channel closed, so that their redelivery is acknowledged without running the handler again.
/// </summary>
/// <param name="capacity">The number of message ids kept. Older ids are forgotten first.</param>
internal sealed class RabbitMQCompletedDeliveries(int capacity)
{
    private readonly object _sync = new();
    private readonly Dictionary<string, LinkedListNode<string>> _nodes = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _order = new();

    /// <summary>
    /// Remembers that the handler of the message completed.
    /// </summary>
    public void Add(string messageId)
    {
        lock (_sync)
        {
            if (_nodes.ContainsKey(messageId))
            {
                return;
            }

            _nodes[messageId] = _order.AddLast(messageId);

            if (_order.Count > capacity)
            {
                _nodes.Remove(_order.First!.Value);
                _order.RemoveFirst();
            }
        }
    }

    /// <summary>
    /// Forgets the message, and returns whether its handler completed before.
    /// </summary>
    public bool TryRemove(string messageId)
    {
        lock (_sync)
        {
            if (!_nodes.Remove(messageId, out var node))
            {
                return false;
            }

            _order.Remove(node);
            return true;
        }
    }
}
