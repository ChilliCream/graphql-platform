using System;

namespace HotChocolate.Language
{
    public sealed class VariableNode
        : IValueNode<string>
    {
        public VariableNode(
            NameNode name)
            : this(null, name)
        {
        }

        public VariableNode(
            Location location,
            NameNode name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Location = location;
            Name = name;
        }

        public NodeKind Kind { get; } = NodeKind.Variable;

        public Location Location { get; }

        public NameNode Name { get; }

        public string Value => Name.Value;
    }
}
