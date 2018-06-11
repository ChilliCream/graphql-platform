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
            if (value == null)
            {
                convertedValue = null;
                return true;
            }

            if (from == typeof(DateTimeOffset)
                && to == typeof(DateTime)
                && value is DateTimeOffset d)
            {
                convertedValue = d.ToLocalTime().DateTime;
                return true;
            }

            convertedValue = null;
            return true;
        }
    }
}
