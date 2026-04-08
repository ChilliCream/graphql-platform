using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Utilities;

public class NullableTypeConverter : IChangeTypeProvider
{
    public bool TryCreateConverter(
        Type source,
        Type target,
        ChangeTypeProvider root,
        [NotNullWhen(true)] out ChangeType? converter)
    {
        var innerFrom = GetUnderlyingNullableType(source);
        var innerTo = GetUnderlyingNullableType(target);

        if ((innerFrom != source || innerTo != target)
            && root(innerFrom, innerTo, out converter))
        {
            return true;
        }

        converter = null;
        return false;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
        Justification = "The type is always Nullable<T> which is well-known to the trimmer.")]
    private Type GetUnderlyingNullableType(Type type)
    {
        if (type.IsValueType && type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var nullableConverter = new NullableConverter(type);
            return nullableConverter.UnderlyingType;
        }
        return type;
    }
}
