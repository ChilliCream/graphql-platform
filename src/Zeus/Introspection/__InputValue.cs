using Zeus.Abstractions;

namespace Zeus.Introspection
{
    internal class __InputValue
    {
        private __InputValue(string name, string description, IType type, string defaultValue)
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

        public static __InputValue Create(string name, string description, IType type, string defaultValue)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (type == null)
            {
                throw new System.ArgumentNullException(nameof(type));
            }

            return new __InputValue(name, description, type, defaultValue);
        }
    }
}