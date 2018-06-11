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
            if (from == typeof(double)
                && (to == typeof(float) || to == typeof(float?)))
            {
                if (value is double d)
                {
                    convertedValue = (float)d;
                    return true;
                }

                if (value == null && to == typeof(float))
                {
                    convertedValue = default(float);
                    return true;
                }

                if (value == null && to == typeof(float?))
                {
                    convertedValue = default(float?);
                    return true;
                }
            }

            convertedValue = null;
            return false;
        }
    }
}
