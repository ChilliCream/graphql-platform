#nullable enable

namespace HotChocolate.Utilities;

public interface ITypeConverter
{
    bool TryConvert(Type from, Type to, object? source, out object? converted);

    object? Convert(Type from, Type to, object? source);
}
