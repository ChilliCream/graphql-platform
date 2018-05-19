using HotChocolate.Abstractions;

namespace HotChocolate.Introspection
{
    internal class __InputValue
    {
        internal __InputValue(string name, string description, IType type, string defaultValue)
        {
            Name = name;
            Description = description;
            Type = type;
            DefaultValue = defaultValue;
        }

        public string Name { get; }
        public string Description { get; }
        public IType Type { get; }
        public string DefaultValue { get; }
    }
}