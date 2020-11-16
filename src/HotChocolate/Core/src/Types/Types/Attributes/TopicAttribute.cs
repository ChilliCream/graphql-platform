using System;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
    public sealed class TopicAttribute : Attribute
    {
        public TopicAttribute(string? name = null)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the constant topic name that shall be used to receive messages.
        /// </summary>
        public string? Name { get; }
    }
}
