namespace Zeus.Abstractions
{
    public class Argument
    {
        private string _stringRepresentation;

        public Argument(string name, IValue value)
        {
            if (name == null)
            {
                throw new System.ArgumentNullException(nameof(name));
            }

            if (value == null)
            {
                throw new System.ArgumentNullException(nameof(value));
            }

            Name = name;
            Value = value;
        }

        public string Name { get; }

        public IValue Value { get; }

        public override string ToString()
        {
            if (_stringRepresentation == null)
            {
                _stringRepresentation = $"{Name}: {Value}";
            }
            return _stringRepresentation;
        }
    }
}