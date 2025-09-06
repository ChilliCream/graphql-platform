#nullable disable

namespace HotChocolate.Utilities;

public static class TypeConverterExtensions
{
    public static bool TryConvert(
        this ITypeConverter typeConverter,
        Type to,
        object source,
        out object converted) =>
        typeConverter.TryConvert(typeof(object), to, source, out converted);

    public static bool TryConvert<TFrom, TTo>(
        this ITypeConverter typeConverter,
        TFrom source,
        out TTo converted) =>
        TryConvert(typeConverter, source, out converted, out _);

    public static bool TryConvert<TFrom, TTo>(
        this ITypeConverter typeConverter,
        TFrom source,
        out TTo converted,
        out Exception conversionException)
    {
        ArgumentNullException.ThrowIfNull(typeConverter);

        if (typeConverter.TryConvert(
            typeof(TFrom), typeof(TTo),
            source, out var c,
            out conversionException)
            && c is TTo convertedCasted)
        {
            converted = convertedCasted;
            return true;
        }

        converted = default;
        return false;
    }

    public static TTo Convert<TFrom, TTo>(
        this ITypeConverter typeConverter,
        object source)
    {
        ArgumentNullException.ThrowIfNull(typeConverter);

        return (TTo)typeConverter.Convert(
            typeof(TFrom), typeof(TTo), source);
    }
}
