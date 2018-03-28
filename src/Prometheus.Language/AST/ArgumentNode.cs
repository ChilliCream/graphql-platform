namespace Prometheus.Language
{
    public class ArgumentNode
        : ISyntaxNode
    {
        public ArgumentNode(Location location, NameNode name, IValueNode value)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            Location = location;
            Name = name;
            Value = value;
        }

        public NodeKind Kind { get; } = NodeKind.Argument;
        public Location Location { get; }
        public NameNode Name { get; }
        public IValueNode Value { get; }
    }
}