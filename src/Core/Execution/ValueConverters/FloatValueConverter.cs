using System;
using HotChocolate.Types;

namespace HotChocolate.Execution.ValueConverters
{
    internal class FloatValueConverter
        : IInputValueConverter
    {
        public bool CanConvert(IInputType inputType)
        {
            return inputType is FloatType;
        }

        public bool TryConvert(Type from, Type to, object value, out object convertedValue)
        {
            if (value == null)
            {
                convertedValue = null;
                return true;
            }

            if (from == typeof(double)
                && to == typeof(float)
                && value is double d)
            {
                convertedValue = (float)d;
                return true;
            }

            convertedValue = null;
            return true;
        }
    }
}
