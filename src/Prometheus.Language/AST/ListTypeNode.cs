namespace Prometheus.Language
{
    public class ListTypeNode
        : INullableType
    {
        public ListTypeNode(Location location, ITypeNode type)
        {
            if (type == null)
            {
                throw new System.ArgumentNullException(nameof(type));
            }

            Location = location;
            Type = type;
        }

        public NodeKind Kind { get; } = NodeKind.ListType;
        public Location Location { get; }
        public ITypeNode Type { get; }
    }
}