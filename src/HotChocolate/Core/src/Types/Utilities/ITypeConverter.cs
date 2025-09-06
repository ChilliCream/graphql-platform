namespace HotChocolate.Utilities;

public interface ITypeConverter
{
    bool TryConvert(Type from, Type to, object? source, out object? converted, out Exception? conversionException)
    {
        conversionException = null;
        return TryConvert(from, to, source, out converted);
    }

    bool TryConvert(Type from, Type to, object? source, out object? converted) ;

    object? Convert(Type from, Type to, object? source);
}
