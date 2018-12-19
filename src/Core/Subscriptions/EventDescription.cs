using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Subscriptions
{
    public sealed class EventDescription
        : IEventDescription
        , IEquatable<EventDescription>
    {
        private string _serialized;

        public EventDescription(string name)
            : this(name, Array.Empty<ArgumentNode>())
        {
        }

        public EventDescription(string name,
            IEnumerable<ArgumentNode> arguments)
            : this(name, arguments.ToArray())
        {
        }

        public EventDescription(string name, params ArgumentNode[] arguments)
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

        public bool Equals(EventDescription other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Name.Equals(other.Name, StringComparison.Ordinal)
                && Arguments.Count == other.Arguments.Count)
            {
                Dictionary<string, IValueNode> arguments =
                    other.Arguments.ToDictionary(
                        c => c.Name.Value,
                        c => c.Value);

                foreach (ArgumentNode argument in Arguments)
                {
                    if (!arguments.TryGetValue(argument.Name.Value, out var v)
                        || !v.Equals(argument.Value))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as EventDescription);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Name.GetHashCode() * 379;
                foreach (ArgumentNode argument in Arguments)
                {
                    hash ^= (argument.Name.GetHashCode() * 7);
                    hash ^= (argument.Value.GetHashCode() * 11);
                }
                return hash;
            }
        }

        public override string ToString()
        {
            if (_serialized == null)
            {
                if (Arguments.Any())
                {
                    _serialized = $"{Name}({SerializeArguments(Arguments)})";
                }
                else
                {
                    _serialized = Name;
                }
            }
            return _serialized;
        }

        private static string SerializeArguments(
            IEnumerable<ArgumentNode> arguments)
        {
            var serializer = new QuerySyntaxSerializer();
            var sb = new StringBuilder();

            using (var stringWriter = new StringWriter(sb))
            {
                var documentWriter = new DocumentWriter(stringWriter);

                return string.Join(", ", arguments.Select(t =>
                {
                    sb.Clear();
                    serializer.Visit(t.Value, documentWriter);
                    return t.Name.Value + " = " + sb.ToString();
                }));
            }
        }
    }
}

