using System;
using HotChocolate.Types;

namespace HotChocolate.Execution.ValueConverters
{
    internal class DateTimeValueConverter
        : IInputValueConverter
    {
        public bool CanConvert(IInputType inputType)
        {
            return inputType is DateTimeType;
        }

        public bool TryConvert(Type from, Type to, object value, out object convertedValue)
        {
            if (from == typeof(DateTimeOffset)
                && (to == typeof(DateTime) || to == typeof(DateTime?)))
            {
                if (value is DateTimeOffset d)
                {
                    convertedValue = d.ToLocalTime().DateTime;
                    return true;
                }

                if (value == null && to == typeof(DateTime))
                {
                    convertedValue = default(DateTime);
                    return true;
                }

                if (value == null && to == typeof(DateTime?))
                {
                    convertedValue = null;
                    return true;
                }
            }

            convertedValue = null;
            return false;
        }
    }
}
