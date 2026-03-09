namespace HotChocolate.Utilities;

/// <summary>
/// Defines a contract for converting an object from one type to another.
/// </summary>
public interface ITypeConverter
{
    /// <summary>
    /// Attempts to convert the given source object from one type to another.
    /// </summary>
    /// <param name="from">The source type to convert from.</param>
    /// <param name="to">The target type to convert to.</param>
    /// <param name="source">The object to be converted.</param>
    /// <param name="converted">The converted object, if successful.</param>
    /// <param name="conversionException">The exception encountered during the conversion attempt (if any).</param>
    /// <returns>True if the conversion was successful; otherwise, false.</returns>
    bool TryConvert(Type from, Type to, object? source, out object? converted, out Exception? conversionException);

    /// <summary>
    /// Converts an object from one type to another.
    /// Throws an exception if the conversion is not possible.
    /// </summary>
    /// <param name="from">The source type to convert from.</param>
    /// <param name="to">The target type to convert to.</param>
    /// <param name="source">The object to be converted.</param>
    /// <returns>The converted object.</returns>
    object? Convert(Type from, Type to, object? source);
}
