using System;
using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public sealed class ArgumentNode
        : ISyntaxNode
    {
        public ArgumentNode(Location location, NameNode name, IValueNode value)
        {
            Location = location;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public NodeKind Kind { get; } = NodeKind.Argument;

        public Location Location { get; }

        public NameNode Name { get; }

        public IValueNode Value { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;
            yield return Value;
        }

        public ArgumentNode WithLocation(Location location)
        {
            return new ArgumentNode(location, Name, Value);
        }

        public ArgumentNode WithName(NameNode name)
        {
            return new ArgumentNode(Location, name, Value);
        }

        public ArgumentNode WithValue(IValueNode value)
        {
            return new ArgumentNode(Location, Name, value);
        }
    }
}
