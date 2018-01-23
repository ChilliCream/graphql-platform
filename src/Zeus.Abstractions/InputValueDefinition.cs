using System;
using System.Text;

namespace Zeus.Abstractions
{
    public class InputValueDefinition
         : IFieldDefinition
    {
        private string _stringRepresentation = null;

        public InputValueDefinition(string name, IType type, IValue defaultValue)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("A type definition name must not be null or empty.", nameof(name));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
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
                StringBuilder sb = new StringBuilder();

                sb.Append($"{Name}: {Type}");

                if (DefaultValue != null)
                {
                    sb.Append($" = {DefaultValue}");
                }

                _stringRepresentation = sb.ToString();
            }

            return _stringRepresentation;
        }
    }
}