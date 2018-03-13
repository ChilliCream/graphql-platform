
namespace Prometheus.Introspection
{
    internal class __EnumValue
    {
        internal __EnumValue(string name, string description,
            bool isDeprecated, string deprecationReason)
        {
            Name = name;
            Description = description;
            IsDeprecated = isDeprecated;
            DeprecationReason = deprecationReason;
        }

        public string Name { get; }
        public string Description { get; }
        public bool IsDeprecated { get; }
        public string DeprecationReason { get; }
    }
}