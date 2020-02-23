using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Subscriptions
{
    [Obsolete("Use HotChocolate.Subscriptions.IEventStream<TMessage>.")]
    public class EventMessage
        : IEventMessage
    {
        public EventMessage(IEventDescription eventDescription)
            : this(eventDescription, null)
        {
        }

        public EventMessage(IEventDescription eventDescription, object payload)
        {
            Event = eventDescription
                ?? throw new ArgumentNullException(nameof(eventDescription));
            Payload = payload;
        }

        public EventMessage(string name)
            : this(name, Array.Empty<ArgumentNode>())
        {
        }

        public EventMessage(string name, object payload)
            : this(name, Array.Empty<ArgumentNode>())
        {
            Payload = payload;
        }

        public EventMessage(string name, IEnumerable<ArgumentNode> arguments)
            : this(name, arguments.ToArray())
        {
        }

        public EventMessage(string name, params ArgumentNode[] arguments)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The event name cannot be null or empty.",
                    nameof(name));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            Event = new EventDescription(name, arguments);
        }

        public IEventDescription Event { get; }

        public object Payload { get; }
    }
}

