using System;

namespace HotChocolate.Utilities
{
    public static class TypeConversionExtensions
    {
        public static bool TryConvert<TFrom, TTo>(
            this ITypeConversion typeConversion,
            TFrom source, out TTo converted)
        {
            if (typeConversion == null)
            {
                throw new ArgumentNullException(nameof(typeConversion));
            }

            if (typeConversion.TryConvert(
                typeof(TFrom), typeof(TTo),
                source, out object conv)
                && conv is TTo convcasted)
            {
                converted = convcasted;
                return true;
            }

            converted = default;
            return false;
        }

        public static TTo Convert<TFrom, TTo>(
            this ITypeConversion typeConversion,
            object source)
        {
            if (typeConversion == null)
            {
                throw new ArgumentNullException(nameof(typeConversion));
            }

            return (TTo)typeConversion.Convert(
                typeof(TFrom), typeof(TTo), source);
        }
    }
}
