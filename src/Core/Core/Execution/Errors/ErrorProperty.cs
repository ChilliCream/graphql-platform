using System;

namespace HotChocolate.Execution
{
    public class ErrorProperty
    {
        public ErrorProperty(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "The name of an error property cannot be null or empty.",
                    nameof(name));
            }

            Name = name;
            Value = value;
        }

        public string Name { get; }

        public object Value { get; }
    }
}
