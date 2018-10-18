using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Subscriptions
{
    public class EventMessage
        : IEventMessage
    {
        public EventMessage(IEventDescription eventDescription)
        {
            Event = eventDescription 
                ?? throw new ArgumentNullException(nameof(eventDescription));
        }

        public EventMessage(string name)
            : this(name, Array.Empty<ArgumentNode>())
        {
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
    }
}

