using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Subscriptions
{
    /// <summary>
    /// Describes an event in the GraphQL schema.
    /// </summary>
    [Obsolete("Use HotChocolate.Subscriptions.IEventStream<TMessage>.")]
    public interface IEventDescription
    {
        /// <summary>
        /// Gets the event name.
        /// </summary>
        /// <value>
        /// The event name.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Gets the event arguments values.
        /// </summary>
        /// <value>
        /// The event arguments.
        /// </value>
        IReadOnlyList<ArgumentNode> Arguments { get; }
    }
}

