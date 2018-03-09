using System;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    internal class EnumValueConverter
        : IValueConverter
    {
        public T Convert<T>(IValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is EnumValue ev)
            {
                if (typeof(string) == typeof(T) 
                    || typeof(object) == typeof(T))
                {
                    return (T)value.Value;
                }

                if (typeof(T).IsEnum)
                {
                    return (T)System.Enum.Parse(typeof(T), ev.Value, true);
                }

                return (T)System.Convert.ChangeType(ev.Value, typeof(T));
            }

            throw new ArgumentException(
                "The specified type is not an enum value type.",
                nameof(value));
        }
    }
}