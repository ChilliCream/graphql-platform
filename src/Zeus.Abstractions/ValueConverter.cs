using System;

namespace Zeus.Abstractions
{
    public static class ValueConverter
    {
        public static T ConvertTo<T>(IValue value)
        {
            if (value is NullValue)
            {
                return default(T); // this might be problematic.
            }

            if (value is IntegerValue iv)
            {
                return ConvertScalar<T, int>(iv);
            }

            if (value is FloatValue fv)
            {
                return ConvertScalar<T, decimal>(fv);
            }

            if (value is StringValue sv)
            {
                return ConvertScalar<T, string>(sv);
            }

            if (value is BooleanValue bv)
            {
                return ConvertScalar<T, bool>(bv);
            }
         
            throw new NotSupportedException();
        }

        private static T ConvertScalar<T, TScalar>(ScalarValue<TScalar> scalarValue)
        {
            if (typeof(T) == typeof(TScalar))
            {
                return (T)(object)scalarValue.Value;
            }
            return (T)Convert.ChangeType(scalarValue.Value, typeof(T));
        }
    }
}