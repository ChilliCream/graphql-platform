using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Subscriptions
{
    public interface ISubscriptionManager
    {
        object Subscribe(QueryRequest queryRequest, string eventName, object args);
        void Unsubscribe(string subscriptionId);
        object Restore(string subscriptionId);
    }





    public sealed class Event
        : IEquatable<Event>
    {
        public Event(string name)
            : this(name, Array.Empty<ArgumentNode>())
        {
        }

        public Event(string name, IEnumerable<ArgumentNode> arguments)
            : this(name, arguments.ToArray())
        {
        }

        public Event(string name, params ArgumentNode[] arguments)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The event name cannot be null or empty.",
                    nameof(name));
            }

            Name = name;
            Arguments = arguments
                ?? throw new ArgumentNullException(nameof(arguments));
        }

        public string Name { get; }

        public IReadOnlyCollection<ArgumentNode> Arguments { get; }

        public bool Equals(Event other)
        {
            throw new NotImplementedException();
        }
    }
}

