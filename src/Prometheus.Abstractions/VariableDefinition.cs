namespace Prometheus.Abstractions
{
    public class VariableDefinition
    {
        private string _stringRepresentation;

        public VariableDefinition(string name, IType type)
            : this(name, type, null)
        {
        }

        public VariableDefinition(string name, IType type, IValue defaultValue)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (type == null)
            {
                throw new System.ArgumentNullException(nameof(type));
            }

            Name = name;
            Type = type;
            DefaultValue = defaultValue;
        }

        public string Name { get; }

        public IType Type { get; }

        public IValue DefaultValue { get; }

        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                string s = $"${Name}: {Type}";
                if (DefaultValue != null)
                {
                    s = $"{s} = {DefaultValue}";
                }
                _stringRepresentation = s;
            }
            return _stringRepresentation;
        }
    }
}