using System;

namespace HotChocolate.Language
{
    public sealed class NameNode
        : ISyntaxNode
    {
        public NameNode(
            Location location,
            string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "The value of a name node cannot be null or empty.",
                    nameof(value));
            }

            Location = location;
            Value = value;
        }

        public NodeKind Kind { get; } = NodeKind.Name;
        public Location Location { get; }
        public string Value { get; }

        public override string ToString()
        {
            return $"Name: {Value}";
        }
    }
}