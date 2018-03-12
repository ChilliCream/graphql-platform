using System;

namespace Zeus.Abstractions
{
    public sealed class Variable
        : IValue
    {
        public Variable(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return "$" + Name;
        }

        object IValue.Value => Name;
    }
}