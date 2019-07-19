using System.Runtime.InteropServices;
using System;
using System.Buffers;
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
        private static readonly Encoding _encoding = Encoding.UTF8;
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

        private EventDescription(
            string name,
            IReadOnlyList<ArgumentNode> arguments)
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

        public IReadOnlyList<ArgumentNode> Arguments { get; }

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
                var arguments =
                    other.Arguments.ToDictionary(
                        c => c.Name.Value,
                        c => c.Value);

                foreach (ArgumentNode argument in Arguments)
                {
                    if (!arguments.TryGetValue(argument.Name.Value,
                        out IValueNode v)
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
                if (Arguments.Count == 0)
                {
                    _serialized = Name;
                }
                else
                {
                    var serialized = new StringBuilder();
                    serialized.Append(Name);
                    serialized.Append('(');
                    SerializeArguments(serialized, Arguments);
                    serialized.Append(')');
                    _serialized = serialized.ToString();
                }
            }
            return _serialized;
        }

        private static void SerializeArguments(
            StringBuilder serialized,
            IReadOnlyList<ArgumentNode> arguments)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                if (i != 0)
                {
                    serialized.Append(',');
                    serialized.Append(' ');
                }

                ArgumentNode argument = arguments[i];
                serialized.Append(argument.Name.Value);
                serialized.Append(':');
                serialized.Append(' ');
                serialized.Append(QuerySyntaxSerializer.Serialize(argument.Value));
            }
        }

        public static EventDescription Parse(string s)
        {
            return Parse(_encoding.GetBytes(s));
        }

        public static EventDescription Parse(ReadOnlySpan<byte> data)
        {
            var reader = new Utf8GraphQLReader(data);
            if (reader.Read())
            {
                if (reader.Kind != TokenKind.Name)
                {
                    // TODO : exception
                    throw new Exception();
                }

                string name = reader.GetString();

                var parser = new Utf8GraphQLParser(
                    reader,
                    ParserOptions.NoLocation);
                IReadOnlyList<ArgumentNode> arguments = parser.ParseArguments();
                return new EventDescription(name, arguments);
            }

            throw new ArgumentException("data is empty.", nameof(data));
        }
    }
}

