using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

#nullable enable

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
