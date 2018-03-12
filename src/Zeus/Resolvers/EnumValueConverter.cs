using System;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    internal class EnumValueConverter
        : IValueConverter
    {
        public object Convert(IValue value, Type desiredType)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is EnumValue ev)
            {
                if (typeof(string) == desiredType
                    || typeof(object) == desiredType)
                {
                    return value.Value;
                }

                if (desiredType.IsEnum)
                {
                    return System.Enum.Parse(desiredType, ev.Value, true);
                }

                return System.Convert.ChangeType(ev.Value, desiredType);
            }

            throw new ArgumentException(
                "The specified type is not an enum value type.",
                nameof(value));
        }
    }
}