
using System;

namespace Prometheus.Abstractions
{
    public sealed class EnumValue
       : IValue
    {
        public EnumValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "A enum value mustn't be null or empty.",
                    nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        object IValue.Value => Value;
    }
}